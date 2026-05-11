import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { CheckCircle2, XCircle, Loader2, Link2, Link2Off } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import './Settings.css';

interface SocialAccount {
  platform: string;
  displayName: string;
  connectedAtUtc: string;
}

const PLATFORMS = [
  { id: 'linkedin', label: 'LinkedIn' },
  { id: 'instagram', label: 'Instagram', comingSoon: true },
  { id: 'tiktok', label: 'TikTok', comingSoon: true },
];

export default function Settings() {
  const [accounts, setAccounts] = useState<SocialAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [disconnecting, setDisconnecting] = useState<string | null>(null);
  const [searchParams, setSearchParams] = useSearchParams();

  const flashConnected = searchParams.get('connected');
  const flashError = searchParams.get('error');

  useEffect(() => {
    loadAccounts();
  }, []);

  useEffect(() => {
    if (flashConnected || flashError) {
      const timer = setTimeout(() => setSearchParams({}, { replace: true }), 4000);
      return () => clearTimeout(timer);
    }
  }, [flashConnected, flashError, setSearchParams]);

  const loadAccounts = async () => {
    setLoading(true);
    try {
      const res = await fetch('/api/social/accounts', { credentials: 'include' });
      if (res.ok) setAccounts(await res.json());
    } finally {
      setLoading(false);
    }
  };

  const connect = (platform: string) => {
    globalThis.location.href = `/api/social/connect/${platform}`;
  };

  const disconnect = async (platform: string) => {
    setDisconnecting(platform);
    try {
      const res = await fetch(`/api/social/disconnect/${platform}`, {
        method: 'DELETE',
        credentials: 'include',
      });
      if (res.ok) setAccounts(prev => prev.filter(a => a.platform !== platform));
    } finally {
      setDisconnecting(null);
    }
  };

  const getAccount = (platform: string) =>
    accounts.find(a => a.platform === platform);

  return (
    <div className="settings-shell animate-fade-in">
      <div className="settings-header">
        <h1>Connected Accounts</h1>
        <p className="muted">Connect your social accounts once — we remember them for all future sessions.</p>
      </div>

      {flashConnected && (
        <motion.div
          className="alert success"
          initial={{ opacity: 0, y: -8 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <CheckCircle2 size={16} />
          {flashConnected.charAt(0).toUpperCase() + flashConnected.slice(1)} connected successfully.
        </motion.div>
      )}

      {flashError && (
        <motion.div
          className="alert error"
          initial={{ opacity: 0, y: -8 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <XCircle size={16} />
          {flashError === 'linkedin_denied'
            ? 'LinkedIn access was denied.'
            : 'Failed to connect. Please try again.'}
        </motion.div>
      )}

      {loading ? (
        <div className="settings-loading">
          <Loader2 className="spin" size={24} />
        </div>
      ) : (
        <div className="platform-list">
          {PLATFORMS.map(({ id, label, comingSoon }) => {
            const account = getAccount(id);
            const isDisconnecting = disconnecting === id;
            let platformStatus = <span className="platform-sub muted">Not connected</span>;
            if (account) {
              platformStatus = <span className="platform-user">{account.displayName}</span>;
            } else if (comingSoon) {
              platformStatus = <span className="platform-sub muted">Coming soon</span>;
            }

            return (
              <motion.div
                key={id}
                className={`platform-card ${account ? 'connected' : ''} ${comingSoon ? 'coming-soon' : ''}`}
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
              >
                <div className="platform-info">
                  <div className={`platform-icon ${id}`} />
                  <div>
                    <span className="platform-name">{label}</span>
                    {platformStatus}
                  </div>
                </div>

                <div className="platform-action">
                  {account ? (
                    <>
                      <span className="badge-connected">
                        <CheckCircle2 size={13} /> Connected
                      </span>
                      <button
                        className="btn-ghost danger"
                        onClick={() => disconnect(id)}
                        disabled={isDisconnecting}
                      >
                        {isDisconnecting
                          ? <Loader2 size={14} className="spin" />
                          : <Link2Off size={14} />}
                        Disconnect
                      </button>
                    </>
                  ) : (
                    <button
                      className="btn-primary"
                      onClick={() => connect(id)}
                      disabled={!!comingSoon}
                    >
                      <Link2 size={14} /> Connect
                    </button>
                  )}
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
    </div>
  );
}
