import React from 'react';
import QueryBuilder from './QueryBuilder';

function QueryBuilderPage() {
  return (
    <div className="query-builder-page">
      <main className="qb-page-main">
        <header className="qb-page-header">
          <h1>Query Builder</h1>
          <p className="qb-page-subtitle">Build Sumo Logic queries visually or in code</p>
        </header>
        <QueryBuilder />
      </main>
    </div>
  );
}

export default QueryBuilderPage;
