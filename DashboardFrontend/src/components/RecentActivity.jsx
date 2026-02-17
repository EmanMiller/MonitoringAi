import React, { useState, useEffect } from 'react';
import { getRecentActivity } from '../services/api';

const ICON_BY_TYPE = {
  query_run: '🔍',
  dashboard_update: '📊',
  confluence_created: '📖',
};

function RecentActivity() {
  const [activities, setActivities] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    getRecentActivity(20)
      .then((res) => {
        if (cancelled) return;
        const list = Array.isArray(res?.activities) ? res.activities : res?.Activities ?? [];
        setActivities(list);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err.message || 'Failed to load activity');
          setActivities([]);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, []);

  return (
    <div className="recent-activity-card">
      <h2 className="recent-activity-card__title">Recent Activity</h2>
      {loading && (
        <p className="recent-activity-card__state">Loading…</p>
      )}
      {error && (
        <p className="recent-activity-card__state recent-activity-card__state--error" role="alert">
          {error}
        </p>
      )}
      {!loading && !error && activities.length === 0 && (
        <p className="recent-activity-card__state">No recent activity</p>
      )}
      {!loading && !error && activities.length > 0 && (
        <ul className="recent-activity-card__list" aria-label="Recent activities">
          {activities.map((item) => (
            <li key={item.id} className="recent-activity-card__item">
              <span className="recent-activity-card__icon" aria-hidden>
                {ICON_BY_TYPE[item.type] ?? '•'}
              </span>
              <span className="recent-activity-card__description">{item.description}</span>
              <span className="recent-activity-card__time">{item.timeAgo}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default RecentActivity;
