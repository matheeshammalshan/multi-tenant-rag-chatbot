import { useEffect, useState } from 'react';
import { useToast } from '../contexts/useToast';
import { organizations as orgApi } from '../services';
import type { OrganizationSummaryDto } from '../types';

export function OrganizationsPage() {
  const { showToast } = useToast();
  const [orgs, setOrgs] = useState<OrganizationSummaryDto[]>([]);
  const [_loading, setLoading] = useState(true);

  useEffect(() => {
    orgApi
      .list()
      .then(setOrgs)
      .catch(err => showToast(err.message, 'error'))
      .finally(() => setLoading(false));
  }, [showToast]);


  return (
    <section className="page on" id="orgs">
      <div className="head">
        <div>
          <h1>Organizations</h1>
          <p className="sub">Organizations are collected during user registration.</p>
        </div>
      </div>
      <div className="card table-scroll">
        <table>
          <thead><tr><th>Organization</th><th>Contact</th><th>Chatbots</th></tr></thead>
          <tbody id="orgRows">
            {orgs.map(o => (
              <tr key={o.id}>
                <td><b>{(o.name)}</b></td>
                <td>{(o.email)}<div className="sub">{(o.phoneNumber)}</div></td>
                <td>{o.chatbotCount}</td>
              </tr>
            ))}
            {orgs.length === 0 && (
              <tr><td colSpan={3} className="sub" style={{ padding: '28px', textAlign: 'center' }}>No organizations found.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
