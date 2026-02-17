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

  const PLACEHOLDER_COUNT = 5;

  return (
    <div className={`recent-activity-card${loading ? ' recent-activity-card--loading' : ''}`}>
      <h2 className="recent-activity-card__title">Recent Activity</h2>
      {loading && (
        <ul className="recent-activity-card__list" aria-label="Loading recent activities">
          {Array.from({ length: PLACEHOLDER_COUNT }, (_, i) => (
            <li
              key={`placeholder-${i}`}
              className="recent-activity-card__item recent-activity-card__item--placeholder"
              data-intro-index={i}
            >
              <span className="recent-activity-card__icon recent-activity-card__icon--placeholder" aria-hidden />
              <span className="recent-activity-card__description recent-activity-card__description--placeholder" />
              <span className="recent-activity-card__time recent-activity-card__time--placeholder" />
            </li>
          ))}
        </ul>
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
