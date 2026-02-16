import React, { useState } from 'react';

const DashboardWizard = ({ onComplete }) => {
  const [step, setStep] = useState(1);
  const [dashboardTitle, setDashboardTitle] = useState('');
  const [useDefaults, setUseDefaults] = useState(true);
  const [templateVars, setTemplateVars] = useState({
    domain_prefix: 'www',
    environment: 'prod',
    domain: 'crateandbarrel',
    top_level_domain: 'com',
    timeslice: '1m',
    time_shift: '1d',
  });
  const [panels, setPanels] = useState([]);

  const handleNext = () => setStep(step + 1);
  const handleBack = () => setStep(step - 1);

  const handleGenerate = () => {
    const wizardData = {
      dashboardTitle,
      useDefaults,
      templateVars,
      panels,
    };
    onComplete(wizardData);
  };

  return (
    <div className="wizard">
      {step === 1 && (
        <div className="wizard-step">
          <h3>Step 1: Dashboard Title</h3>
          <input
            type="text"
            placeholder="Enter Dashboard Title"
            value={dashboardTitle}
            onChange={(e) => setDashboardTitle(e.target.value)}
          />
          <button onClick={handleNext}>Next</button>
        </div>
      )}

      {step === 2 && (
        <div className="wizard-step">
          <h3>Step 2: Template Variables</h3>
          <div className="toggle-container">
            <label>Use Recommended Defaults</label>
            <input
              type="checkbox"
              checked={useDefaults}
              onChange={() => setUseDefaults(!useDefaults)}
            />
          </div>
          {useDefaults ? (
            <p>Using Defaults (www, prod, crateandbarrel, com, 1m timeslice, 1d shift)</p>
          ) : (
            <div>
              {/* Add select dropdowns for each var */}
            </div>
          )}
          <button onClick={handleBack}>Back</button>
          <button onClick={handleNext}>Next</button>
        </div>
      )}

      {step === 3 && (
        <div className="wizard-step">
          <h3>Step 3: Panel Selection</h3>
          <div className="checkbox-group">
            <label>
              <input type="checkbox" name="panels" value="Success Rate %" /> Success Rate %
            </label>
            <label>
              <input type="checkbox" name="panels" value="Error Rate %" /> Error Rate %
            </label>
            <label>
              <input type="checkbox" name="panels" value="Past 7 day trend" /> Past 7 day trend
            </label>
          </div>
          <button onClick={handleBack}>Back</button>
          <button onClick={handleGenerate}>Generate</button>
        </div>
      )}
    </div>
  );
};

export default DashboardWizard;
