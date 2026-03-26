import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Clock, Tag, AlertCircle } from 'lucide-react';
import { readApiError, normalizeError } from '../utils/api';
import './MyPosts.css';

interface CaptionDto {
  variantIndex: number;
  text: string;
}

interface PostDto {
  id: string;
  platform: string;
  status: string;
  createdAtUtc: string;
  captions: CaptionDto[];
}

export default function MyPosts() {
  const [posts, setPosts] = useState<PostDto[] | null>(null);
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

              <div className="captions-stack">
                {post.captions.map(cap => (
                  <div key={cap.variantIndex} className="caption-entry">
                    <span className="variant-label">
                      <Tag size={12} /> Option {cap.variantIndex + 1}
                    </span>
                    <p className="caption-body">{cap.text}</p>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
