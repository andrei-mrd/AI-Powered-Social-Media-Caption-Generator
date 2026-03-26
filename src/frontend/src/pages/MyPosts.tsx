import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Clock, Tag, AlertCircle, CalendarClock, Wand2 } from 'lucide-react';
import { readApiError, normalizeError } from '../utils/api';
import './MyPosts.css';

interface CaptionDto {
  variantIndex: number;
  text: string;
  isSelected?: boolean;
}

interface PostDto {
  id: string;
  platform: string;
  status: string;
  createdAtUtc: string;
  captions: CaptionDto[];
  scheduledAtUtc?: string | null;
}

interface MediaItem {
  id: string;
  type: string;
  url: string;
  createdAtUtc: string;
}

export default function MyPosts() {
  const [posts, setPosts] = useState<PostDto[] | null>(null);
  const [media, setMedia] = useState<MediaItem[]>([]);
  const [scheduleAt, setScheduleAt] = useState<Record<string, string>>({});
  const [mediaSelection, setMediaSelection] = useState<Record<string, string>>({});
  const [selectedCaption, setSelectedCaption] = useState<Record<string, number>>({});
  const [scheduleError, setScheduleError] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    const load = async () => {
      try {
        const res = await fetch('/api/posts', { credentials: 'include' });
        if (res.status === 401) {
          navigate('/login');
          return;
        }
        if (!res.ok) throw new Error(await readApiError(res, 'Unable to load posts'));
        const data = await res.json();
        setPosts(data);
      } catch (err) {
        setError(normalizeError(err, 'Unable to load your posts'));
      }
    };
    load();
  }, [navigate]);

  useEffect(() => {
    const loadMedia = async () => {
      try {
        const res = await fetch('/api/media', { credentials: 'include' });
        if (!res.ok) return; // media optional
        const data = await res.json();
        setMedia(data);
      } catch {
        // ignore
      }
    };
    loadMedia();
  }, []);

  const platformIcon: Record<string, string> = {
    instagram: '📷',
    tiktok: '🎵',
    linkedin: '💼',
  };

  const handleSelectCaption = async (postId: string, variantIndex: number) => {
    setSelectedCaption(prev => ({ ...prev, [postId]: variantIndex }));
    try {
      const res = await fetch(`/api/posts/${postId}/select-caption/${variantIndex}`, {
        method: 'POST',
        credentials: 'include'
      });
      if (!res.ok) throw new Error(await readApiError(res, 'Selection failed'));
      setPosts(prev => prev?.map(p => p.id === postId ? {
        ...p,
        captions: p.captions.map(c => ({ ...c, isSelected: c.variantIndex === variantIndex }))
      } : p) ?? prev);
    } catch (err) {
      setError(normalizeError(err, 'Unable to select caption'));
    }
  };

  const handleSchedule = async (postId: string) => {
    setScheduleError('');
    const when = scheduleAt[postId];
    if (!when) {
        setScheduleError('Please pick a schedule time.');
        return;
    }
    const mediaId = mediaSelection[postId];
    const selectedIdx = selectedCaption[postId];

    try {
      const res = await fetch(`/api/posts/${postId}/schedule`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          scheduledAtUtc: when,
          selectedCaptionIndex: Number.isInteger(selectedIdx) ? selectedIdx : undefined,
          mediaIds: mediaId ? [mediaId] : []
        })
      });
      if (!res.ok) throw new Error(await readApiError(res, 'Schedule failed'));

      setPosts(prev => prev?.map(p => p.id === postId ? { ...p, status: 'scheduled', scheduledAtUtc: when } : p) ?? prev);
    } catch (err) {
      setScheduleError(normalizeError(err, 'Unable to schedule post'));
    }
  };

  if (error) return (
    <div className="posts-container animate-fade-in">
      <div className="error-state">
        <AlertCircle size={32} />
        <p>{error}</p>
      </div>
    </div>
  );

  return (
    <div className="posts-container animate-fade-in">
      <header className="posts-header">
        <div>
          <h1>My Posts</h1>
          <p className="subtitle">Your generated caption history</p>
        </div>
      </header>

      {posts === null ? (
        <div className="loading-grid">
          {[1, 2, 3].map(i => <div key={i} className="skeleton" />)}
        </div>
      ) : posts.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">📁</div>
          <h3>No posts yet</h3>
          <p>Head over to <a href="/generate">Generate</a> to create your first post.</p>
        </div>
      ) : (
        <div className="posts-list">
          {posts.map(post => (
            <div key={post.id} className="post-card">
              <div className="post-card-header">
                <div className="platform-pill">
                  <span>{platformIcon[post.platform] ?? '📱'}</span>
                  <span className="platform-name">{post.platform}</span>
                </div>
                <div className="post-meta-right">
                  <span className={`status-badge ${post.status}`}>{post.status}</span>
                  <span className="post-date">
                    <Clock size={13} />
                    {new Date(post.createdAtUtc + 'Z').toLocaleDateString('en-US', {
                      month: 'short', day: 'numeric', year: 'numeric'
                    })}
                  </span>
                </div>
              </div>

              <div className="post-body">
                <div className="captions-stack">
                  {post.captions.map(cap => (
                    <div key={cap.variantIndex} className="caption-entry">
                      <span className="variant-label">
                        <Tag size={12} /> Option {cap.variantIndex + 1}
                      </span>
                      <p className="caption-body">{cap.text}</p>
                      <div className="schedule-actions">
                        <button
                          className="btn-secondary"
                          type="button"
                          onClick={() => handleSelectCaption(post.id, cap.variantIndex)}
                        >
                          <Wand2 size={14} /> {cap.isSelected ? 'Selected' : 'Select this'}
                        </button>
                        {cap.isSelected && <span className="status-pill">Preferred variant</span>}
                      </div>
                    </div>
                  ))}
                </div>

                <div className="schedule-box">
                  <h4><CalendarClock size={16} /> Schedule</h4>
                  <div className="schedule-row">
                    <div>
                      <label className="helper">Publish at (your local time)</label>
                      <input
                        type="datetime-local"
                        className="input-inline"
                        value={scheduleAt[post.id] || ''}
                        onChange={(e) => setScheduleAt(prev => ({ ...prev, [post.id]: e.target.value }))}
                      />
                    </div>
                    <div>
                      <label className="helper">Attach media (optional)</label>
                      <select
                        className="select-inline"
                        value={mediaSelection[post.id] || ''}
                        onChange={(e) => setMediaSelection(prev => ({ ...prev, [post.id]: e.target.value }))}
                      >
                        <option value="">None</option>
                        {media.map(m => (
                          <option key={m.id} value={m.id}>
                            {m.type} • {new Date(m.createdAtUtc + 'Z').toLocaleDateString('en-US')}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                  <div className="schedule-actions">
                    {scheduleError && <span className="helper" style={{ color: 'var(--text-danger, #dc2626)' }}>{scheduleError}</span>}
                    <button className="btn-primary" type="button" onClick={() => handleSchedule(post.id)}>
                      Schedule post
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
