import { useEffect, useState, useRef } from 'react';
import { Camera, Film, Upload, Trash2, AlertCircle, Loader2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { readApiError, normalizeError } from '../utils/api';
import './MediaLibrary.css';

interface MediaItem {
  id: string;
  type: string;
  url: string;
  createdAtUtc: string;
}

export default function MediaLibrary() {
  const [items, setItems] = useState<MediaItem[] | null>(null);
  const [error, setError] = useState('');
  const [uploadError, setUploadError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const navigate = useNavigate();

  const load = async () => {
    setIsLoading(true);
    setError('');
    try {
      const res = await fetch('/api/media', { credentials: 'include' });
      if (res.status === 401) {
        navigate('/login');
        return;
      }
      if (!res.ok) throw new Error(await readApiError(res, 'Unable to load media'));
      const data = await res.json();
      setItems(data);
    } catch (err) {
      setError(normalizeError(err, 'Unable to load media'));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleUpload = async (event: React.FormEvent) => {
    event.preventDefault();
    const fileInput = fileInputRef.current;
    if (!fileInput || !fileInput.files || fileInput.files.length === 0) return;

    const file = fileInput.files[0];
    setIsUploading(true);
    setUploadError('');

    try {
      const form = new FormData();
      form.append('file', file);
      const res = await fetch('/api/media/upload', {
        method: 'POST',
        body: form,
        credentials: 'include'
      });
      if (res.status === 401) {
        navigate('/login');
        return;
      }
      if (!res.ok) throw new Error(await readApiError(res, 'Upload failed'));
      await load();
      fileInput.value = '';
    } catch (err) {
      setUploadError(normalizeError(err, 'Upload failed'));
    } finally {
      setIsUploading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Delete this media file?')) return;
    try {
      const res = await fetch(`/api/media/${id}`, { method: 'DELETE', credentials: 'include' });
      if (res.status === 401) {
        navigate('/login');
        return;
      }
      if (!res.ok) throw new Error(await readApiError(res, 'Delete failed'));
      setItems(items => items ? items.filter(i => i.id !== id) : items);
    } catch (err) {
      setError(normalizeError(err, 'Delete failed'));
    }
  };

  const formatDate = (value: string) =>
    new Date(value + 'Z').toLocaleString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });

  const formatSizeHint = 'Max 20MB · jpg, png, webp, mp4, mov';

  return (
    <div className="media-layout animate-fade-in">
      <header className="media-header">
        <div>
          <h1>Media Library</h1>
          <p className="subtitle">Upload images or short clips to pair with your captions.</p>
        </div>
        <div className="hint-chip">{formatSizeHint}</div>
      </header>

      <form className="upload-card" onSubmit={handleUpload}>
        <div className="upload-left">
          <div className="upload-icon">
            <Upload size={18} />
          </div>
          <div>
            <p className="upload-title">Upload a file</p>
            <p className="upload-subtitle">We’ll store locally and serve via /media URLs.</p>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/png,image/jpeg,image/webp,video/mp4,video/quicktime"
              aria-label="Select media file"
            />
          </div>
        </div>
        <div className="upload-actions">
          {uploadError && <div className="alert error">{uploadError}</div>}
          <button type="submit" className="btn-upload" disabled={isUploading}>
            {isUploading ? <><Loader2 className="spin" size={16} /> Uploading…</> : 'Upload'}
          </button>
        </div>
      </form>

      {error && (
        <div className="error-banner">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {isLoading ? (
        <div className="loading-grid">
          {[1,2,3,4].map(i => <div key={i} className="skeleton" />)}
        </div>
      ) : items === null || items.length === 0 ? (
        <div className="empty-media">
          <div className="empty-icon"><Camera size={20} /></div>
          <h3>No media yet</h3>
          <p>Upload an image or clip to start pairing with your posts.</p>
        </div>
      ) : (
        <div className="media-grid">
          {items.map(item => (
            <div key={item.id} className="media-card">
              <div className="media-thumb">
                {item.type === 'video' ? (
                  <video src={item.url} controls preload="metadata" />
                ) : (
                  <img src={item.url} alt="" loading="lazy" />
                )}
                <span className="media-badge">
                  {item.type === 'video' ? <Film size={14} /> : <Camera size={14} />}
                  {item.type}
                </span>
                <button className="delete-btn" type="button" onClick={() => handleDelete(item.id)} aria-label="Delete media">
                  <Trash2 size={16} />
                </button>
              </div>
              <div className="media-meta">
                <span className="meta-label">Uploaded</span>
                <span className="meta-value">{formatDate(item.createdAtUtc)}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
