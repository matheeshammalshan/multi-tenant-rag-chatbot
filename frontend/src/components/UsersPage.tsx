import { useEffect, useState } from 'react';
import { useAuth } from '../contexts/useAuth';
import { useToast } from '../contexts/useToast';
import { users as usersApi, organizations as orgApi } from '../services';
import type { UserDto, OrganizationSummaryDto } from '../types';

export function UsersPage() {
  const { user: currentUser } = useAuth();
  const { showToast } = useToast();
  const [users, setUsers] = useState<UserDto[]>([]);
  const [orgs, setOrgs] = useState<OrganizationSummaryDto[]>([]);
  const [_loading, setLoading] = useState(true);

  useEffect(() => {
    if (currentUser?.role !== 'owner') return;
    Promise.all([usersApi.list(), orgApi.list()])
      .then(([u, o]) => { setUsers(u); setOrgs(o); })
      .catch(err => showToast(err.message, 'error'))
      .finally(() => setLoading(false));
  }, [currentUser, showToast]);


  const stats = [
    ['Accounts', users.length],
    ['Organizations', orgs.length],
    ['Users', users.filter(u => u.role === 'user').length],
    ['Owners', users.filter(u => u.role === 'owner').length],
  ] as const;

  return (
    <section className="page on" id="users">
      <div className="head">
        <div>
          <h1>Users &amp; organizations</h1>
          <p className="sub">View every user and registered organization.</p>
        </div>
      </div>
      <div className="grid g4" id="userStats" style={{ marginBottom: 16 }}>
        {stats.map(([label, n]) => (
          <div key={label} className="card stat"><small>{label}</small><div className="n">{n}</div></div>
        ))}
      </div>
      <div className="card table-scroll">
        <table>
          <thead><tr>
            <th>User</th><th>Role</th><th>Registered organization</th><th>Contact</th><th>Chatbots</th><th>Registered</th>
          </tr></thead>
          <tbody id="userRows">
            {users.map(u => {
              const org = orgs.find(o => o.id === u.organizationId);
              return (
                <tr key={u.id}>
                  <td><b>{(u.name)}</b><div className="sub">{(u.email)}</div></td>
                  <td><span className={`chip ${u.role === 'owner' ? 'ok' : ''}`}>{(u.role)}</span></td>
                  <td>{(org?.name || 'Platform account')}</td>
                  <td>{(u.phoneNumber || 'Not provided')}</td>
                  <td>{u.chatbotCount}</td>
                  <td>{new Date(u.createdAtUtc).toLocaleDateString()}</td>
                </tr>
              );
            })}
            {users.length === 0 && (
              <tr><td colSpan={6} className="sub" style={{ padding: '28px', textAlign: 'center' }}>No users found.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export function OrganizationsPage() {
  const { showToast } = useToast();
  const [orgs, setOrgs] = useState<OrganizationSummaryDto[]>([]);
  const [_loading2, setLoading2] = useState(true);

  useEffect(() => {
    orgApi
      .list()
      .then(setOrgs)
      .catch(err => showToast(err.message, 'error'))
      .finally(() => setLoading2(false));
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
