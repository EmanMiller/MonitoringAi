import React, { useState, useEffect } from 'react';
import { createDashboardFromWizard } from '../services/api';

const WizardStep1 = ({ title, setTitle }) => (
  <div className="wizard-step">
    <h3>Dashboard Title</h3>
    <p>Give your new dashboard a clear and descriptive title.</p>
    <input
      type="text"
      value={title}
      onChange={(e) => setTitle(e.target.value)}
      placeholder="e.g., 'Production API Success Metrics'"
    />
  </div>
);

const WizardStep2 = ({ useDefaults, setUseDefaults, variables, setVariables }) => (
    <div className="wizard-step">
        <h3>⚙️ Template Variables</h3>
        <p>Use our recommended defaults or customize the variables for your dashboard.</p>
        <div className="toggle-switch-container" onClick={() => setUseDefaults(!useDefaults)}>
            <div className="toggle-switch">
                <input 
                    type="checkbox" 
                    id="defaults-toggle" 
                    checked={useDefaults} 
                    readOnly
                />
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
                    <select value={variables.timeslice} onChange={(e) => setVariables({...variables, timeslice: e.target.value})}>
                        <option value="5m">5 minutes</option>
                        <option value="15m">15 minutes</option>
                        <option value="30m">30 minutes</option>
                        <option value="1h">1 hour</option>
                    </select>
                </div>
                <div className="variable-item">
                    <label>Domain</label>
                    <select value={variables.domain} onChange={(e) => setVariables({...variables, domain: e.target.value})}>
                        <option value="example.com">example.com</option>
                        <option value="another.com">another.com</option>
                    </select>
                </div>
                 <div className="variable-item">
                    <label>Domain Prefix</label>
                    <select value={variables.domainPrefix} onChange={(e) => setVariables({...variables, domainPrefix: e.target.value})}>
                        <option value="www">www</option>
                        <option value="api">api</option>
                    </select>
                </div>
                 <div className="variable-item">
                    <label>Environment</label>
                    <select value={variables.environment} onChange={(e) => setVariables({...variables, environment: e.target.value})}>
                        <option value="prod">prod</option>
                        <option value="staging">staging</option>
                    </select>
                </div>
            </div>
        )}
    </div>
);

const WizardStep3 = ({ panels, setPanels }) => {
    const handlePanelChange = (panel) => {
        setPanels(prev => ({ ...prev, [panel]: !prev[panel] }));
    };

    return (
        <div className="wizard-step">
            <h3>Panel Selection</h3>
            <p>Choose the initial panels for your dashboard.</p>
            <div className="checkbox-group">
                <label>
                    <input type="checkbox" checked={panels['Success Rate %']} onChange={() => handlePanelChange('Success Rate %')} />
                    Success Rate %
                </label>
                <label>
                    <input type="checkbox" checked={panels['Error Rate %']} onChange={() => handlePanelChange('Error Rate %')} />
                    Error Rate %
                </label>
                <label>
                    <input type="checkbox" checked={panels['Past 7 day trend']} onChange={() => handlePanelChange('Past 7 day trend')} />
                    Past 7 day trend
                </label>
            </div>
        </div>
    );
};

const ProcessingView = ({ status }) => (
    <div className="processing-view">
        <h3>AI Assistant at Work...</h3>
        <p dangerouslySetInnerHTML={{ __html: status }}></p>
        <div className="spinner"></div>
    </div>
);

const DashboardCreatorWizard = ({ isOpen, onClose }) => {
  const [step, setStep] = useState(1);
  const [dashboardTitle, setDashboardTitle] = useState('');
  const [useDefaults, setUseDefaults] = useState(true);
  const [variables, setVariables] = useState({
      timeslice: '15m',
      domain: 'example.com',
      domainPrefix: 'www', // Corrected to camelCase
      environment: 'prod',
  });
  const [panels, setPanels] = useState({
    'Success Rate %': true,
    'Error Rate %': true,
    'Past 7 day trend': false,
  });
  const [isGenerating, setIsGenerating] = useState(false);
  const [statusMessage, setStatusMessage] = useState('');

  useEffect(() => {
    if (!isOpen) {
      // Reset state when closed
      setStep(1);
      setDashboardTitle('');
      setUseDefaults(true);
      setIsGenerating(false);
      setStatusMessage('');
    }
  }, [isOpen]);
  
  const handleGenerate = async () => {
    setIsGenerating(true);
    setStatusMessage("⚙️ Validating configuration...");

    const payload = {
      dashboardTitle,
      useDefaults,
      variables,
      panels,
    };

    try {
        await new Promise(resolve => setTimeout(resolve, 1000));
        setStatusMessage("🔍 Injecting variables into Sumo Logic templates...");
        
        const response = await createDashboardFromWizard(payload);
        const dashboardUrl = response.dashboardUrl;

        await new Promise(resolve => setTimeout(resolve, 1000));
        setStatusMessage("🚀 Creating Dashboard in Sumo Logic...");

        await new Promise(resolve => setTimeout(resolve, 1000));
        setStatusMessage("📝 Updating Confluence Page...");

        await new Promise(resolve => setTimeout(resolve, 1000));
        setStatusMessage(`✅ Dashboard Live! <a href="${dashboardUrl}" target="_blank" rel="noopener noreferrer">View Dashboard</a>`);

    } catch (error) {
        let errorMessage = error.response?.data?.details || error.message;
        setStatusMessage(`❌ Error: ${errorMessage}. Please try again.`);
    }
  };
  
  const handleNext = () => setStep(s => Math.min(s + 1, 3));
  const handleBack = () => setStep(s => Math.max(s - 1, 1));

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <div className="modal-header">
          <h2>{isGenerating ? 'Generating Dashboard' : '📊 Create a New Dashboard'}</h2>
          {!isGenerating && <button onClick={onClose} className="close-button">&times;</button>}
        </div>
        <div className="modal-body">
            {isGenerating ? (
                <ProcessingView status={statusMessage} />
            ) : (
                <>
                    {step === 1 && <WizardStep1 title={dashboardTitle} setTitle={setDashboardTitle} />}
                    {step === 2 && <WizardStep2 useDefaults={useDefaults} setUseDefaults={setUseDefaults} variables={variables} setVariables={setVariables} />}
                    {step === 3 && <WizardStep3 panels={panels} setPanels={setPanels} />}
                </>
            )}
        </div>
        <div className="modal-footer">
            {!isGenerating && (
                <>
                    {step > 1 && <button className="button-secondary" onClick={handleBack}>Back</button>}
                    {step < 3 ? (
                        <button className="button-primary" onClick={handleNext}>Next</button>
                    ) : (
                        <button className="button-primary" onClick={handleGenerate} disabled={!dashboardTitle}>Generate</button>
                    )}
                </>
            )}
             {(isGenerating && (statusMessage.startsWith("✅") || statusMessage.startsWith("❌"))) && (
                <button className="button-primary" onClick={onClose}>Done</button>
            )}
        </div>
      </div>
    </div>
  );
};

export default DashboardCreatorWizard;

