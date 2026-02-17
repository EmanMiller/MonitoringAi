import React, { useRef, useEffect } from 'react';

function CodeMode({ query, onChange, onBlur }) {
  const textareaRef = useRef(null);

  useEffect(() => {
    const ta = textareaRef.current;
    if (!ta) return;
    const handleKeyDown = (e) => {
      if (e.key === 'Tab') {
        e.preventDefault();
        const start = ta.selectionStart;
        const end = ta.selectionEnd;
        const before = ta.value.slice(0, start);
        const after = ta.value.slice(end);
        const indent = '  ';
        ta.value = before + indent + after;
        ta.selectionStart = ta.selectionEnd = start + indent.length;
        onChange(ta.value);
      }
    };
    ta.addEventListener('keydown', handleKeyDown);
    return () => ta.removeEventListener('keydown', handleKeyDown);
  }, [onChange]);

  return (
    <div className="query-builder-code">
      <div className="qb-code-header">Sumo Logic Query</div>
      <div className="qb-code-wrap">
        <div className="qb-line-numbers" aria-hidden>
          {(query || '').split('\n').map((_, i) => (
            <div key={i} className="qb-line-num">{i + 1}</div>
          ))}
          {(!query || !query.trim()) && <div className="qb-line-num">1</div>}
        </div>
        <textarea
          ref={textareaRef}
          value={query || ''}
          onChange={(e) => onChange(e.target.value)}
          onBlur={onBlur}
          spellCheck={false}
          placeholder="* | where status=&quot;error&quot; | count by _sourceCategory | limit 100"
          className="qb-textarea"
        />
      </div>
    </div>
  );
}

export default CodeMode;
