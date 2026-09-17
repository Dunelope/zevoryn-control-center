import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import type { FeedbackReport } from '../lib/types';

const types = ['All', 'Bug', 'Suggestion', 'Other'] as const;
const statuses = ['All', 'New', 'InProgress', 'Resolved'] as const;

export function FeedbackReports() {
  const [reports, setReports] = useState<FeedbackReport[]>([]);
  const [type, setType] = useState<(typeof types)[number]>('All');
  const [status, setStatus] = useState<(typeof statuses)[number]>('All');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      setError('');
      setReports(await api.feedbackReports(status === 'All' ? '' : status, type === 'All' ? '' : type));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [status, type]);

  async function update(report: FeedbackReport, next: FeedbackReport['status']) {
    try {
      setError('');
      const updated = await api.updateFeedbackReportStatus(report.id, next);
      setReports(items => items.map(item => item.id === updated.id ? updated : item));
    } catch (e) {
      setError((e as Error).message);
    }
  }

  return <section>
    <div className="page-heading"><div><p className="eyebrow">CleanersFlow</p><h2>Feedback Reports</h2></div></div>
    {error && <p className="error">{error}</p>}
    <div className="filters card">
      <label>Type<select value={type} onChange={e => setType(e.target.value as typeof type)}>{types.map(value => <option key={value}>{value}</option>)}</select></label>
      <label>Status<select value={status} onChange={e => setStatus(e.target.value as typeof status)}>{statuses.map(value => <option key={value}>{value}</option>)}</select></label>
    </div>
    {loading ? <p className="muted">Loading reports…</p> : reports.length === 0 ? <div className="card"><p className="muted">No feedback reports match these filters.</p></div> : <div className="feedback-list">
      {reports.map(report => <article className="card feedback-report" key={report.id}>
        <div className="feedback-header"><div><span className="eyebrow">{report.type}</span><h3>{report.title}</h3><p className="muted">{report.companyName} · {report.submitterName} · <a href={`mailto:${report.submitterEmail}`}>{report.submitterEmail}</a></p></div>
          <select aria-label={`Status for ${report.title}`} value={report.status} onChange={e => void update(report, e.target.value as FeedbackReport['status'])}>{statuses.slice(1).map(value => <option key={value}>{value}</option>)}</select>
        </div>
        <p className="feedback-description">{report.description}</p>
        <div className="feedback-meta"><span>Page: <code>{report.currentPageRoute}</code></span><span>Submitted: {new Date(report.createdAtUtc).toLocaleString()}</span></div>
      </article>)}
    </div>}
  </section>;
}
