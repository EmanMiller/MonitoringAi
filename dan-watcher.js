require('dotenv').config(); // Load .env automatically
const chokidar = require('chokidar');
const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');

// Load credentials from .env
const { GITHUB_USERNAME, GITHUB_EMAIL, GITHUB_TOKEN } = process.env;

if (!GITHUB_TOKEN || GITHUB_TOKEN === 'your_github_token_here') {
  console.error('❌ GITHUB_TOKEN not found or still placeholder in .env file');
  process.exit(1);
}

console.log(`👤 Git user: ${GITHUB_USERNAME} <${GITHUB_EMAIL}>`);

// Coordination: push to main only after BOTH @Becca AND @Paul approve (set .dan-approved when both have)
const HOLD_FILE = path.join(__dirname, '.dan-hold');       // @Dan wait → no push
const APPROVED_FILE = path.join(__dirname, '.dan-approved'); // Both approved → allow push on main

function isHold() {
  try { return fs.existsSync(HOLD_FILE); } catch { return false; }
}
function isApproved() {
  if (process.env.DAN_APPROVED === '1' || process.env.DAN_APPROVED === 'true') return true;
  try { return fs.existsSync(APPROVED_FILE); } catch { return false; }
}

// Configuration - watch frontend and API
const WATCH_DIRS = ['DashboardFrontend/src', 'DashboardFrontend/public', 'DashboardApi'];
const IGNORE_PATTERNS = ['node_modules', '.git', 'dist', 'build', '.env', 'bin', 'obj', '.dan-hold', '.dan-approved'];
const DEBOUNCE_MS = 5000; // Wait 5s after last change

let changeTimeout;
let changedFiles = new Set();

// Configure git on startup
exec(`git config user.name "${GITHUB_USERNAME}"`, (err) => {
  if (err) console.error('Git config name failed:', err);
});

exec(`git config user.email "${GITHUB_EMAIL}"`, (err) => {
  if (err) console.error('Git config email failed:', err);
});

console.log('🔍 Dan is watching for file changes...');

// Initialize watcher
const watcher = chokidar.watch(WATCH_DIRS, {
  ignored: IGNORE_PATTERNS,
  persistent: true,
  ignoreInitial: true
});

watcher
  .on('add', path => handleChange('added', path))
  .on('change', path => handleChange('modified', path))
  .on('unlink', path => handleChange('deleted', path));

function handleChange(event, filepath) {
  console.log(`📝 File ${event}: ${filepath}`);
  changedFiles.add(filepath);

  clearTimeout(changeTimeout);
  changeTimeout = setTimeout(() => {
    commitAndPush();
  }, DEBOUNCE_MS);
}

function commitAndPush() {
  if (changedFiles.size === 0) return;

  const files = Array.from(changedFiles);
  const message = generateCommitMessage(files);

  console.log(`\n💾 Committing ${files.length} file(s)...`);

  exec('git add .', (err) => {
    if (err) {
      console.error('❌ Git add failed:', err);
      return;
    }

    exec(`git commit -m "${message}"`, (err, stdout) => {
      if (err) {
        if (err.message.includes('nothing to commit')) {
          console.log('ℹ️  No changes to commit');
        } else {
          console.error('❌ Commit failed:', err);
        }
        changedFiles.clear();
        return;
      }

      console.log('✅ Committed:', message);
      changedFiles.clear();

      pushToGitHub(message);
    });
  });
}

function generateCommitMessage(files) {
  const categories = {
    component: [],
    page: [],
    api: [],
    style: [],
    test: [],
    config: [],
    other: []
  };

  files.forEach(file => {
    if (file.includes('component') || file.includes('Component')) categories.component.push(file);
    else if (file.includes('page') || file.includes('Page')) categories.page.push(file);
    else if (file.includes('api') || file.includes('Api') || file.includes('Service')) categories.api.push(file);
    else if (file.includes('style') || file.includes('.css') || file.includes('.scss')) categories.style.push(file);
    else if (file.includes('Test') || file.includes('.test.')) categories.test.push(file);
    else if (file.includes('config') || file.includes('.json')) categories.config.push(file);
    else categories.other.push(file);
  });

  let message = '[DAN] ';
  const details = [];

  if (categories.component.length) details.push(`${categories.component.length} component(s)`);
  if (categories.page.length) details.push(`${categories.page.length} page(s)`);
  if (categories.api.length) details.push(`${categories.api.length} API file(s)`);
  if (categories.style.length) details.push(`${categories.style.length} style(s)`);
  if (categories.test.length) details.push(`${categories.test.length} test(s)`);
  if (categories.config.length) details.push(`${categories.config.length} config(s)`);

  message += details.length ? details.join(', ') : `${files.length} file(s) updated`;

  return message;
}

function pushToGitHub(commitMessage) {
  const msg = commitMessage || '[DAN] update';

  exec('git branch --show-current', (err, branchStdout) => {
    const currentBranch = (branchStdout || 'main').trim() || 'main';

    if (isHold()) {
      console.log('⏸️  @Dan wait: holding push. Commit is local only.');
      return;
    }
    if (currentBranch === 'main' && !isApproved()) {
      console.log('⏳ On main: waiting for @Becca approval before pushing. Commit is local only.');
      return;
    }

    console.log('🚀 Pushing to GitHub...');
    const authRepo = `https://${GITHUB_USERNAME}:${GITHUB_TOKEN}@github.com/EmanMiller/MonitoringAi.git`;

    exec(`git push ${authRepo} ${currentBranch}`, (err, stdout, stderr) => {
      if (err) {
        const errMsg = (stderr || err.message || String(err)).trim();
        console.error('❌ Push failed:', errMsg, '- @Paul or @Becca please check');
        exec('git push', (err2) => {
          if (err2) console.error('❌ Push retry failed:', err2.message, '- @Paul or @Becca please check');
          else console.log('✅ Pushed to GitHub - Commit:', msg, '- Branch:', currentBranch);
        });
        return;
      }

      console.log('✅ Pushed to GitHub - Commit:', msg, '- Branch:', currentBranch);
    });
  });
}

// Auto-commit every 5 minutes if changes exist
setInterval(() => {
  exec('git status --porcelain', (err, stdout) => {
    if (err) return;

    if (stdout.trim()) {
      console.log('\n⏰ Auto-commit interval (5min)...');
      commitAndPush();
    }
  });
}, 300000);

// Graceful shutdown
process.on('SIGINT', () => {
  console.log('\n\n🛑 Dan is shutting down...');

  if (changedFiles.size > 0) {
    console.log('💾 Committing pending changes...');
    commitAndPush();

    setTimeout(() => {
      watcher.close();
      process.exit(0);
    }, 2000);
  } else {
    watcher.close();
    process.exit(0);
  }
});
