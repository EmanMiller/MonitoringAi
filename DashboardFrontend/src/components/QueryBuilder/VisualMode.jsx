import React from 'react';
import {
  FIELDS,
  OPERATORS,
  TIME_PRESETS,
  LIMIT_OPTIONS,
  AGG_FUNCTIONS,
  GROUP_BY_FIELDS,
} from './queryBuilderLogic';

function VisualMode({ visual, onChange }) {
  const update = (key, value) => onChange({ ...visual, [key]: value });

  const addFilter = () => {
    update('filters', [...(visual.filters || []), { field: '', operator: '=', value: '' }]);
  };

  const updateFilter = (idx, part, val) => {
    const filters = [...(visual.filters || [])];
    if (!filters[idx]) filters[idx] = { field: '', operator: '=', value: '' };
    filters[idx] = { ...filters[idx], [part]: val };
    update('filters', filters);
  };

  const removeFilter = (idx) => {
    const filters = visual.filters.filter((_, i) => i !== idx);
    update('filters', filters.length ? filters : [{ field: '', operator: '=', value: '' }]);
  };

  const filters = visual.filters || [{ field: '', operator: '=', value: '' }];

  return (
    <div className="query-builder-visual">
      <section className="qb-section">
        <h4 className="qb-section-title">Filters</h4>
        <div className="qb-filters">
          {filters.map((f, i) => (
            <div key={i} className="qb-filter-row">
              <select
                value={f.field}
                onChange={(e) => updateFilter(i, 'field', e.target.value)}
                className="qb-select qb-field"
              >
                <option value="">Select field</option>
                {FIELDS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
              <select
                value={f.operator}
                onChange={(e) => updateFilter(i, 'operator', e.target.value)}
                className="qb-select qb-operator"
              >
                {OPERATORS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
              <input
                type="text"
                value={f.value}
                onChange={(e) => updateFilter(i, 'value', e.target.value)}
                placeholder="Value"
                className="qb-input qb-value"
              />
              <button
                type="button"
                className="qb-remove"
                onClick={() => removeFilter(i)}
                aria-label="Remove filter"
              >
                ✕
              </button>
            </div>
          ))}
          <button type="button" className="qb-add-filter" onClick={addFilter}>
            + Add Filter
          </button>
        </div>
      </section>

      <section className="qb-section">
        <h4 className="qb-section-title">Aggregation</h4>
        <div className="qb-aggregation">
          <div className="qb-row">
            <label>Group By</label>
            <select
              value={visual.groupBy || ''}
              onChange={(e) => update('groupBy', e.target.value)}
              className="qb-select"
            >
              {GROUP_BY_FIELDS.map((opt) => (
                <option key={opt.value || 'none'} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          <div className="qb-row">
            <label>Function</label>
            <select
              value={visual.aggFunction || 'count'}
              onChange={(e) => update('aggFunction', e.target.value)}
              className="qb-select"
            >
              {AGG_FUNCTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          {(AGG_FUNCTIONS.find((a) => a.value === visual.aggFunction)?.needsField) && (
            <div className="qb-row">
              <label>Field</label>
              <select
                value={visual.aggField || ''}
                onChange={(e) => update('aggField', e.target.value)}
                className="qb-select"
              >
                <option value="">Select field</option>
                {FIELDS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
          )}
        </div>
      </section>

      <section className="qb-section qb-row-section">
        <div className="qb-row">
          <h4 className="qb-section-title">Time Range</h4>
          <select
            value={visual.timeRange || '1h'}
            onChange={(e) => update('timeRange', e.target.value)}
            className="qb-select"
          >
            {TIME_PRESETS.map((opt) => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
        {visual.timeRange === 'custom' && (
          <div className="qb-custom-time">
            <input
              type="datetime-local"
              value={visual.timeStart || ''}
              onChange={(e) => update('timeStart', e.target.value)}
              className="qb-input"
            />
            <span className="qb-to">to</span>
            <input
              type="datetime-local"
              value={visual.timeEnd || ''}
              onChange={(e) => update('timeEnd', e.target.value)}
              className="qb-input"
            />
          </div>
        )}
      </section>

      <section className="qb-section">
        <div className="qb-row">
          <h4 className="qb-section-title">Limit</h4>
          <select
            value={visual.limit ?? 100}
            onChange={(e) => update('limit', parseInt(e.target.value, 10))}
            className="qb-select"
          >
            {LIMIT_OPTIONS.map((n) => (
              <option key={n} value={n}>{n}</option>
            ))}
          </select>
        </div>
      </section>
    </div>
  );
}

export default VisualMode;
