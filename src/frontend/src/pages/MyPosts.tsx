import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Clock, AlertCircle } from 'lucide-react';
import { readApiError, normalizeError } from '../utils/api';
import './MyPosts.css';

interface PostDto {
  id: string;
  platform: string;
  status: string;
  createdAtUtc: string;
  selectedCaption: string | null;
  scheduledAtUtc?: string | null;
  media: MediaItem[];
}

interface MediaItem {
  id: string;
  type: string;
  url: string;
  createdAtUtc: string;
  captionText?: string;
}

export default function MyPosts() {
  const [posts, setPosts] = useState<PostDto[] | null>(null);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    let isMounted = true;
    const load = async () => {
      try {
        const res = await fetch('/api/posts', { credentials: 'include' });
        if (res.status === 401) {
          navigate('/login');
          return;
        }
        if (!res.ok) throw new Error(await readApiError(res, 'Unable to load posts'));
        const data = await res.json();
        if (isMounted) setPosts(data);
      } catch (err) {
        if (isMounted) setError(normalizeError(err, 'Unable to load your posts'));
      }
    };
    load();
    const interval = window.setInterval(load, 20000); // poll every 20s to reflect status changes
    return () => {
      isMounted = false;
      window.clearInterval(interval);
    };
  }, [navigate]);

  const platformIcon: Record<string, string> = {
    instagram: '📷',
    tiktok: '🎵',
    linkedin: '💼',
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
          <p className="subtitle">Upcoming and published posts</p>
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
          <p>Head over to <a href="/create-post">Create Post</a> to create your first post.</p>
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
                  {post.scheduledAtUtc && (
                    <span className="post-date">
                      <Clock size={13} />
                      {new Date(post.scheduledAtUtc).toLocaleString()}
                    </span>
                  )}
                </div>
              </div>

              <div className="post-body">
                <div className="captions-stack">
                  <div className="caption-entry">
                    <span className="variant-label">Selected caption</span>
                    <p className="caption-body">{post.selectedCaption ?? 'Not selected yet'}</p>
                  </div>
                  {post.media?.length ? (
                    <div className="media-preview">
                      {post.media.map(m => (
                        <div key={m.id} className="media-chip">
                          {m.type} • {new Date(m.createdAtUtc + 'Z').toLocaleDateString('en-US')}
                        </div>
                      ))}
                    </div>
                  ) : null}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
