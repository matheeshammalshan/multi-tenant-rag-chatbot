import { useState, useEffect, useRef } from 'react';
import { useAuth } from '../contexts/useAuth';
import { useToast } from '../contexts/useToast';

export function AuthScreen({ onKeyChat }: { onKeyChat: () => void }) {
  const { login, register, loading } = useAuth();
  const { showToast } = useToast();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [showPw, setShowPw] = useState(false);
  const [error, setError] = useState('');
  const formRef = useRef<HTMLFormElement>(null);

  const showErr = (msg: string) => setError(msg || '');
  const okMail = (v: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    showErr('');
    const fd = new FormData(e.currentTarget);
    const mail = (fd.get('email') as string).trim();
    const pw = fd.get('password') as string;

    if (mode === 'register') {
      const org = (fd.get('org') as string).trim();
      const name = (fd.get('name') as string).trim();
      const tel = (fd.get('phone') as string).trim();
      const pw2 = fd.get('confirm') as string;
      const tos = fd.get('tos') as string;

      if (!org || !name) return showErr('Organization and your name are required.');
      if (!okMail(mail)) return showErr('Enter a valid email address.');
      if (pw.length < 8) return showErr('Password must be at least 8 characters.');
      if (pw !== pw2) return showErr('Passwords do not match.');
      if (!tos) return showErr('Please accept the terms to continue.');
      try {
        await register({ organizationName: org, name, email: mail, password: pw, phoneNumber: tel || null, acceptTerms: true });
        showToast('Account registered with your organization. Welcome!');
      } catch (err: any) {
        showErr(err.message);
      }
    } else {
      if (!okMail(mail)) return showErr('Enter a valid email address.');
      if (!pw) return showErr('Enter your password.');
      try {
        await login({ email: mail, password: pw, rememberMe: fd.get('remember') === 'on' });
        showToast('Signed in successfully.');
      } catch (err: any) {
        showErr(err.message);
      }
    }
  };

  const updateMeter = (pw: string) => {
    let s = 0;
    if (pw.length >= 8) s++;
    if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) s++;
    if (/\d/.test(pw)) s++;
    if (/[^A-Za-z\d]/.test(pw)) s++;
    const meter = document.getElementById('meter');
    if (meter) meter.className = 'meter s' + (pw ? Math.max(1, s) : 0);
  };

  useEffect(() => {
    const handleHash = () => {
      if (window.location.hash === '#register') setMode('register');
      else setMode('login');
    };
    handleHash();
    window.addEventListener('hashchange', handleHash);
    return () => window.removeEventListener('hashchange', handleHash);
  }, []);

  return (
    <>
      <section id="auth">
        <div className="hero">
          <div className="brand">
            <div className="logo">R</div>
            <div><b>RAG Console</b><span>Multi-tenant chatbots</span></div>
          </div>
          <div>
            <h2>Chatbots that answer from your own documents.</h2>
            <p>Give every team or customer its own assistant, its own knowledge and its own settings.</p>
          </div>
          <div className="perks">
            <div><i>✓</i><span>Separate workspace for each organization</span></div>
            <div><i>✓</i><span>Upload PDFs and text, searchable in minutes</span></div>
            <div><i>✓</i><span>Choose the model and tune its behaviour</span></div>
            <div><i>✓</i><span>One API key per chatbot to embed anywhere</span></div>
          </div>
        </div>
        <div className="authbox">
          <div className="authcard">
            <h1>{mode === 'register' ? 'Create your account' : 'Welcome back'}</h1>
            <p className="sub">{mode === 'register' ? 'Register with your organization and create your own chatbots.' : 'Sign in to manage your chatbots.'}</p>
            <div className="seg">
              <button type="button" className={mode === 'login' ? 'on' : ''} onClick={() => setMode('login')}>Sign in</button>
              <button type="button" className={mode === 'register' ? 'on' : ''} onClick={() => setMode('register')}>Create account</button>
            </div>
            <div className={`err ${error ? 'on' : ''}`}>{error}</div>
            <form ref={formRef} onSubmit={handleSubmit} noValidate>
              {mode === 'register' && (
                <div id="regFields">
                  <div className="f"><label>Organization name</label><input type="text" name="org" placeholder="Acme Corp" autoComplete="organization" required /></div>
                  <div className="f"><label>Your name</label><input type="text" name="name" placeholder="Jane Perera" autoComplete="name" required /></div>
                  <div className="f"><label>Phone number <span className="sub" style={{ fontWeight: 400 }}>(optional)</span></label><input type="tel" name="phone" placeholder="+94 77 123 4567" autoComplete="tel" /></div>
                </div>
              )}
              <div className="f"><label>Email</label><input type="email" name="email" placeholder="you@company.com" autoComplete="email" required /></div>
              <div className="f">
                <label className="r"><span>Password</span></label>
                <div className="pw">
                  <input
                    type={showPw ? 'text' : 'password'}
                    name="password"
                    placeholder="At least 8 characters"
                    autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
                    required
                    onChange={e => updateMeter(e.target.value)}
                  />
                  <button type="button" onClick={() => setShowPw(!showPw)}>{showPw ? 'Hide' : 'Show'}</button>
                </div>
                <div id="meter" className="meter" style={{ display: mode === 'register' ? 'flex' : 'none' }}><i></i><i></i><i></i><i></i></div>
              </div>
              {mode === 'register' && (
                <div className="f" id="confF">
                  <label>Confirm password</label>
                  <input type="password" name="confirm" placeholder="Repeat password" autoComplete="new-password" required />
                </div>
              )}
              {mode === 'register' && (
                <label className="chk" id="tosRow">
                  <input type="checkbox" name="tos" value="1" required /> I agree to the terms and privacy policy
                </label>
              )}
              {mode === 'login' && (
                <label className="chk" id="remRow">
                  <input type="checkbox" name="remember" defaultChecked /> Keep me signed in
                </label>
              )}
              <button type="submit" className="btn" style={{ width: '100%', justifyContent: 'center', padding: '11px' }} disabled={loading}>
                {loading ? 'Please wait...' : mode === 'register' ? 'Create account' : 'Sign in'}
              </button>
            </form>
            <div className="swap">
              {mode === 'register' ? 'Already registered? ' : 'New here? '}
              <a style={{ cursor: 'pointer' }} onClick={() => setMode(mode === 'register' ? 'login' : 'register')}>{mode === 'register' ? 'Sign in' : 'Create an account'}</a>
            </div>
            <button type="button" className="btn sec" id="guestKeyChat" style={{ width: '100%', justifyContent: 'center', marginTop: '16px' }} onClick={onKeyChat}>
              Chat with an API key
            </button>
          </div>
        </div>
      </section>
    </>
  );
}
