import React, { useState, useEffect, useCallback, useRef } from 'react';
import { createDashboardFromWizard } from '../services/api';
import { sanitizeDashboardName, validateDashboardName } from '../utils/dashboardValidation';

// Recommended defaults with custom alternatives (SumoLogic metrics/fields by category)
const RECOMMENDED_DEFAULTS = [
  {
    id: 'success-rate',
    label: 'Success Rate %',
    queryPreview: '_sourceCategory="prod/cloud/www.*" | success',
    alternatives: ['Request Success %', 'Uptime %', 'Health Check Pass Rate'],
  },
  {
    id: 'error-rate',
    label: 'Error Rate %',
    queryPreview: '_sourceCategory="prod/cloud/www.*" | error',
    alternatives: ['4xx/5xx Rate', 'Exception Count', 'Failed Request %'],
  },
  {
    id: 'slow-queries',
    label: 'Slow Queries',
    queryPreview: '_sourceCategory="prod/cloud/www.*" | _latency > 1000',
    alternatives: ['Query Response Time', 'Database Latency', 'API Timeout Errors', 'Custom Query'],
  },
  {
    id: 'past-7-day',
    label: 'Past 7 day trend',
    queryPreview: '_sourceCategory="prod/cloud/www.*" | timeslice 1d | count by _timeslice',
    alternatives: ['Week-over-Week Change', 'Rolling 7d Average', 'Trend Analysis'],
  },
];

const WizardStep1 = ({ title, setTitle, validation, onValidationChange }) => {
  const handleChange = (e) => {
    const raw = e.target.value;
    const sanitized = sanitizeDashboardName(raw);
    setTitle(sanitized);
    onValidationChange?.(validateDashboardName(sanitized));
  };

  const handleBlur = () => {
    onValidationChange?.(validateDashboardName(title));
  };

  const isValid = validation?.valid ?? false;
  const hasError = validation?.error && title.length > 0;

  return (
    <div className="wizard-step wizard-step-dark">
      <h3>Dashboard Title</h3>
      <p>Give your new dashboard a clear and descriptive title.</p>
      <div className={`dashboard-name-input-wrap ${hasError ? 'invalid' : ''} ${isValid ? 'valid' : ''}`}>
        <input
          type="text"
          value={title}
          onChange={handleChange}
          onBlur={handleBlur}
          placeholder="e.g., Production API Success Metrics"
          maxLength={50}
          className="dashboard-name-input"
        />
        {isValid && <span className="input-check-icon" aria-hidden="true">✓</span>}
        {hasError && <span className="input-error-icon" aria-hidden="true">!</span>}
      </div>
      {validation?.error && <p className="validation-error">{validation.error}</p>}
      {isValid && <p className="validation-success">Dashboard name is valid</p>}
    </div>
  );
};

const CustomSelectionDropdown = ({
  options,
  selected,
  onSelect,
  onClose,
  queryPreview,
  searchPlaceholder = 'Search options...',
}) => {
  const [search, setSearch] = useState('');
  const [highlightIndex, setHighlightIndex] = useState(0);
  const containerRef = useRef(null);
  const listRef = useRef(null);

  const filtered = options.filter((o) =>
    o.toLowerCase().includes(search.toLowerCase())
  );

  useEffect(() => {
    setHighlightIndex(0);
  }, [search]);

  useEffect(() => {
    const handler = (e) => {
      if (!containerRef.current?.contains(e.target)) onClose?.();
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [onClose]);

  useEffect(() => {
    const el = listRef.current?.children[highlightIndex];
    el?.scrollIntoView({ block: 'nearest' });
  }, [highlightIndex, filtered.length]);

  const handleKeyDown = (e) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlightIndex((i) => Math.min(i + 1, filtered.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlightIndex((i) => Math.max(i - 1, 0));
    } else if (e.key === 'Enter' && filtered[highlightIndex]) {
      e.preventDefault();
      e.stopPropagation();
      onSelect(filtered[highlightIndex]);
    } else if (e.key === 'Escape') {
      onClose?.();
    }
  };

  return (
    <div className="custom-selection-dropdown" ref={containerRef}>
      <input
        type="text"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={searchPlaceholder}
        className="dropdown-search-input"
        autoFocus
      />
      <ul className="dropdown-options-list" ref={listRef} role="listbox">
        {filtered.map((opt, i) => (
          <li
            key={opt}
            role="option"
            aria-selected={selected === opt}
            className={i === highlightIndex ? 'highlighted' : ''}
            onClick={() => onSelect(opt)}
          >
            {opt}
          </li>
        ))}
        {filtered.length === 0 && (
          <li className="dropdown-empty">No matches</li>
        )}
      </ul>
      {queryPreview && (
        <div className="dropdown-preview">
          <span className="dropdown-preview-label">Preview:</span>
          <code>{queryPreview}</code>
        </div>
      )}
    </div>
  );
};

const DefaultChip = ({
  item,
  checked,
  customSelection,
  onToggle,
  onCustomSelect,
  onCustomClear,
}) => {
  const [showDropdown, setShowDropdown] = useState(false);

  const handleToggle = () => {
    if (checked) {
      onToggle(false);
      setShowDropdown(true);
    } else {
      onToggle(true);
      onCustomClear?.();
      setShowDropdown(false);
    }
  };

  const handleSelect = (opt) => {
    onCustomSelect?.(opt);
    setShowDropdown(false);
  };

  const displayLabel = checked ? item.label : (customSelection || 'Custom Selection');

  return (
    <div className="default-chip-wrapper">
      <div
        className={`default-chip ${checked ? 'checked' : ''}`}
        onClick={(e) => {
          if (e.target.closest('.default-chip-checkbox')) return;
          if (!e.target.closest('.custom-selection-dropdown')) handleToggle();
        }}
      >
        <label className="default-chip-checkbox" onClick={(e) => e.stopPropagation()}>
          <input
            type="checkbox"
            checked={checked}
            onChange={handleToggle}
            aria-label={`${item.label} - ${checked ? 'using default' : 'custom selection'}`}
          />
        </label>
        <span className="default-chip-label">{displayLabel}</span>
        <button
          type="button"
          className="default-chip-info"
          title={item.queryPreview}
          aria-label="Info"
          onClick={(e) => e.stopPropagation()}
        >
          ℹ
        </button>
      </div>
      {!checked && showDropdown && (
        <CustomSelectionDropdown
          options={item.alternatives}
          selected={customSelection}
          onSelect={handleSelect}
          onClose={() => setShowDropdown(false)}
          queryPreview={item.queryPreview}
        />
      )}
      {!checked && customSelection && !showDropdown && (
        <button
          type="button"
          className="change-custom-btn"
          onClick={() => setShowDropdown(true)}
        >
          Change
        </button>
      )}
    </div>
  );
};

const WizardStep2 = ({ defaults, setDefaults, customSelections, setCustomSelections }) => {
  const handleToggle = (id, checked) => {
    setDefaults((prev) => ({ ...prev, [id]: checked }));
    if (checked) {
      setCustomSelections((prev) => {
        const next = { ...prev };
        delete next[id];
        return next;
      });
    }
  };

  const handleCustomSelect = (id, value) => {
    setCustomSelections((prev) => ({ ...prev, [id]: value }));
  };

  const handleCustomClear = (id) => {
    setCustomSelections((prev) => {
      const next = { ...prev };
      delete next[id];
      return next;
    });
  };

  const selectedCount =
    Object.values(defaults).filter(Boolean).length +
    Object.keys(customSelections).length;
  const isValid = selectedCount >= 1;

  return (
    <div className="wizard-step wizard-step-dark">
      <h3>Recommended Defaults</h3>
      <p>Choose metrics for your dashboard. Uncheck any default to pick an alternative.</p>
      <div className="defaults-chips">
        {RECOMMENDED_DEFAULTS.map((item) => (
          <DefaultChip
            key={item.id}
            item={item}
            checked={defaults[item.id] ?? true}
            customSelection={customSelections[item.id]}
            onToggle={(checked) => handleToggle(item.id, checked)}
            onCustomSelect={(v) => handleCustomSelect(item.id, v)}
            onCustomClear={() => handleCustomClear(item.id)}
          />
        ))}
      </div>
      {!isValid && (
        <p className="validation-error">At least one metric must be selected (default or custom).</p>
      )}
    </div>
  );
};

const WizardStep3 = ({ useDefaults, setUseDefaults, variables, setVariables }) => (
  <div className="wizard-step wizard-step-dark">
    <h3>⚙️ Template Variables</h3>
    <p>Use our recommended defaults or customize the variables for your dashboard.</p>
    <div className="toggle-switch-container" onClick={() => setUseDefaults(!useDefaults)}>
      <div className="toggle-switch">
        <input type="checkbox" id="defaults-toggle" checked={useDefaults} readOnly />
        <label htmlFor="defaults-toggle"></label>
      </div>
      <span className="toggle-label">Use Recommended Defaults</span>
    </div>
    {useDefaults ? (
      <div className="defaults-summary">
        <p><strong>domainPrefix:</strong> www</p>
        <p><strong>environment:</strong> prod</p>
        <p><strong>timeslice:</strong> 15m</p>
        <p><strong>domain:</strong> example.com</p>
      </div>
    ) : (
      <div className="variables-grid">
        <div className="variable-item">
          <label>Timeslice</label>
          <select value={variables.timeslice} onChange={(e) => setVariables({ ...variables, timeslice: e.target.value })}>
            <option value="5m">5 minutes</option>
            <option value="15m">15 minutes</option>
            <option value="30m">30 minutes</option>
            <option value="1h">1 hour</option>
          </select>
        </div>
        <div className="variable-item">
          <label>Domain</label>
          <select value={variables.domain} onChange={(e) => setVariables({ ...variables, domain: e.target.value })}>
            <option value="example.com">example.com</option>
            <option value="another.com">another.com</option>
          </select>
        </div>
        <div className="variable-item">
          <label>Domain Prefix</label>
          <select value={variables.domainPrefix} onChange={(e) => setVariables({ ...variables, domainPrefix: e.target.value })}>
            <option value="www">www</option>
            <option value="api">api</option>
          </select>
        </div>
        <div className="variable-item">
          <label>Environment</label>
          <select value={variables.environment} onChange={(e) => setVariables({ ...variables, environment: e.target.value })}>
            <option value="prod">prod</option>
            <option value="staging">staging</option>
          </select>
        </div>
      </div>
    )}
  </div>
);

const ProcessingView = ({ status }) => (
  <div className="processing-view">
    <h3>AI Assistant at Work...</h3>
    <p>{status}</p>
    <div className="spinner"></div>
  </div>
);

const initialDefaults = Object.fromEntries(
  RECOMMENDED_DEFAULTS.map((d) => [d.id, true])
);

const buildPanelsPayload = (defaults, customSelections) => {
  const panels = {};
  RECOMMENDED_DEFAULTS.forEach((d) => {
    const key = d.label;
    if (defaults[d.id]) {
      panels[key] = true;
    } else if (customSelections[d.id]) {
      panels[key] = false;
      panels[`${key}_custom`] = customSelections[d.id];
    }
  });
  return panels;
};

const DashboardCreatorWizard = ({ isOpen, onClose }) => {
  const [step, setStep] = useState(1);
  const [dashboardTitle, setDashboardTitle] = useState('');
  const [nameValidation, setNameValidation] = useState({ valid: false });
  const [useDefaults, setUseDefaults] = useState(true);
  const [variables, setVariables] = useState({
    timeslice: '15m',
    domain: 'example.com',
    domainPrefix: 'www',
    environment: 'prod',
  });
  const [defaults, setDefaults] = useState(initialDefaults);
  const [customSelections, setCustomSelections] = useState({});
  const [isGenerating, setIsGenerating] = useState(false);
  const [statusMessage, setStatusMessage] = useState('');

  const selectedMetricsCount =
    Object.values(defaults).filter(Boolean).length + Object.keys(customSelections).length;
  const isStep1Valid = nameValidation?.valid ?? false;
  const isStep2Valid = selectedMetricsCount >= 1;
  const canProceedFromStep1 = isStep1Valid;
  const canProceedFromStep2 = isStep2Valid;
  const canGenerate = isStep1Valid && isStep2Valid;

  const handleValidationChange = useCallback((result) => {
    setNameValidation(result);
  }, []);

  useEffect(() => {
    if (!isOpen) {
      setStep(1);
      setDashboardTitle('');
      setNameValidation({ valid: false });
      setUseDefaults(true);
      setDefaults(initialDefaults);
      setCustomSelections({});
      setIsGenerating(false);
      setStatusMessage('');
    }
  }, [isOpen]);

  const handleGenerate = async () => {
    setIsGenerating(true);
    setStatusMessage('⚙️ Validating configuration...');

    const payload = {
      dashboardTitle: dashboardTitle.trim(),
      useDefaults,
      variables,
      panels: buildPanelsPayload(defaults, customSelections),
    };

    try {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      setStatusMessage('🔍 Injecting variables into Sumo Logic templates...');
      await new Promise((resolve) => setTimeout(resolve, 1000));
      setStatusMessage('🚀 Creating Dashboard in Sumo Logic...');

      const response = await createDashboardFromWizard(payload);
      const dashboardUrl = response.dashboardUrl;

      await new Promise((resolve) => setTimeout(resolve, 1000));
      setStatusMessage('📝 Updating Confluence Page...');
      await new Promise((resolve) => setTimeout(resolve, 1000));
      setStatusMessage(
        `✅ Dashboard Live! <a href="${dashboardUrl}" target="_blank" rel="noopener noreferrer">View Dashboard</a>`
      );
    } catch (error) {
      const err = error.response?.data;
      const errorMessage =
        (typeof err === 'object' && err?.details) || (typeof err === 'string' ? err : err?.message) || error.message;
      setStatusMessage(`❌ Error: ${errorMessage}. Please try again.`);
    }
  };

  const handleNext = () => setStep((s) => Math.min(s + 1, 3));
  const handleBack = () => setStep((s) => Math.max(s - 1, 1));

  const getNextDisabled = () => {
    if (step === 1) return !canProceedFromStep1;
    if (step === 2) return !canProceedFromStep2;
    return false;
  };

  const handleKeyDown = (e) => {
    if (e.key !== 'Enter') return;
    if (e.target.closest('.custom-selection-dropdown')) return;
    e.preventDefault();
    if (isGenerating) return;
    if (step < 3 && !getNextDisabled()) {
      handleNext();
    } else if (step === 3 && canGenerate) {
      handleGenerate();
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay wizard-overlay wizard-dark" onKeyDown={handleKeyDown}>
      <div className="modal-content wizard-modal-dark">
        <div className="modal-header">
          <h2>{isGenerating ? 'Generating Dashboard' : '📊 Create a New Dashboard'}</h2>
          {!isGenerating && (
            <button onClick={onClose} className="close-button" aria-label="Close">
              &times;
            </button>
          )}
        </div>
        <div className="modal-body">
          {isGenerating ? (
            <ProcessingView status={statusMessage} />
          ) : (
            <>
              {step === 1 && (
                <WizardStep1
                  title={dashboardTitle}
                  setTitle={setDashboardTitle}
                  validation={nameValidation}
                  onValidationChange={handleValidationChange}
                />
              )}
              {step === 2 && (
                <WizardStep2
                  defaults={defaults}
                  setDefaults={setDefaults}
                  customSelections={customSelections}
                  setCustomSelections={setCustomSelections}
                />
              )}
              {step === 3 && (
                <WizardStep3
                  useDefaults={useDefaults}
                  setUseDefaults={setUseDefaults}
                  variables={variables}
                  setVariables={setVariables}
                />
              )}
            </>
          )}
        </div>
        <div className="modal-footer">
          {!isGenerating && (
            <>
              {step > 1 && (
                <button className="button-secondary" onClick={handleBack}>
                  Back
                </button>
              )}
              {step < 3 ? (
                <button
                  className="button-primary"
                  onClick={handleNext}
                  disabled={getNextDisabled()}
                >
                  Next
                </button>
              ) : (
                <button
                  className="button-primary"
                  onClick={handleGenerate}
                  disabled={!canGenerate}
                >
                  Generate
                </button>
              )}
            </>
          )}
          {isGenerating &&
            (statusMessage.startsWith('✅') || statusMessage.startsWith('❌')) && (
              <button className="button-primary" onClick={onClose}>
                Done
              </button>
            )}
        </div>
      </div>
    </div>
  );
};

export default DashboardCreatorWizard;
