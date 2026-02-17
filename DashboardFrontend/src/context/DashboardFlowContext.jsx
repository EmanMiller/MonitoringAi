import React, { createContext, useContext, useState, useCallback, useRef } from 'react';

const DashboardFlowContext = createContext(null);

export function DashboardFlowProvider({ children }) {
  const [pendingRequest, setPendingRequest] = useState(false);
  const pendingRef = useRef(false);

  const requestDashboardCreation = useCallback(() => {
    pendingRef.current = true;
    setPendingRequest(true);
  }, []);

  const consumeRequest = useCallback(() => {
    if (pendingRef.current) {
      pendingRef.current = false;
      setPendingRequest(false);
      return true;
    }
    return false;
  }, []);

  return (
    <DashboardFlowContext.Provider value={{ requestDashboardCreation, consumeRequest, pendingRequest }}>
      {children}
    </DashboardFlowContext.Provider>
  );
}

export function useDashboardFlow() {
  const ctx = useContext(DashboardFlowContext);
  if (!ctx) throw new Error('useDashboardFlow must be used within DashboardFlowProvider');
  return ctx;
}
