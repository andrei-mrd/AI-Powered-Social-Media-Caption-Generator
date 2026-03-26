import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Camera, Film, ArrowLeft, Sparkles, BookmarkCheck, Tag, Loader2 } from 'lucide-react';
import { readApiError, normalizeError } from '../utils/api';
import './CreatePostFlow.css';

type Step = 'media' | 'content' | 'schedule';

interface MediaItem {
  id: string;
  type: string;
  url: string;
  createdAtUtc: string;
}

interface CreateResponse {
  id: string;
  captions: string[];
  hashtags: string[];
}

export default function CreatePostFlow() {
  const [step, setStep] = useState<Step>('media');
  const [media, setMedia] = useState<MediaItem[]>([]);
  const [selectedMediaId, setSelectedMediaId] = useState<string | null>(null);
  const [description, setDescription] = useState('');
  const [platform, setPlatform] = useState('instagram');
  const [tone, setTone] = useState('professional');
  const [length, setLength] = useState('medium');
  const [isGenerating, setIsGenerating] = useState(false);
  const [generateError, setGenerateError] = useState('');
  const [captions, setCaptions] = useState<string[]>([]);
  const [selectedCaptionIdx, setSelectedCaptionIdx] = useState<number | null>(null);
  const [postId, setPostId] = useState<string | null>(null);
  const [scheduleAt, setScheduleAt] = useState('');
  const [scheduleError, setScheduleError] = useState('');
  const [globalError, setGlobalError] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    const loadMedia = async () => {
      try {
        const res = await fetch('/api/media', { credentials: 'include' });
        if (!res.ok) return;
        const data = await res.json();
        setMedia(data);
      } catch {
        // ignore
      }
    };
    loadMedia();
  }, []);

  const canNextMedia = step === 'media' ? true : true;
  const canNextContent = step === 'content' ? !!postId && selectedCaptionIdx !== null : true;

  const goNext = () => {
    if (step === 'media') setStep('content');
    else if (step === 'content' && canNextContent) setStep('schedule');
  };

  const goPrev = () => {
    if (step === 'schedule') setStep('content');
    else if (step === 'content') setStep('media');
  };

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!description.trim()) {
      setGenerateError('Description is required.');
      return;
    }
    setIsGenerating(true);
    setGenerateError('');
    setGlobalError('');
    setCaptions([]);
    setPostId(null);
    setSelectedCaptionIdx(null);

    try {
      const res = await fetch('/api/posts', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          description,
          platform,
          tone: tone.toLowerCase(),
          language: 'en',
          goal: 'awareness',
          captionLength: length.toLowerCase(),
          includeEmojis: true,
          includeCta: true,
          hashtagCount: 8,
          count: 3
        })
      });
      if (!res.ok) throw new Error(await readApiError(res, 'Generation failed'));
      const data: CreateResponse = await res.json();
      setPostId(data.id);
      setCaptions(data.captions);
      setSelectedCaptionIdx(0);
    } catch (err) {
      setGenerateError(normalizeError(err, 'Unable to generate captions'));
    } finally {
      setIsGenerating(false);
    }
  };

  const handlePost = async () => {
    if (!postId) {
      setScheduleError('Generate and select a caption first.');
      return;
    }
    if (!scheduleAt) {
      setScheduleError('Pick a local publish time.');
      return;
    }
    setScheduleError('');
    setGlobalError('');

    try {
      if (selectedCaptionIdx !== null) {
        const selectRes = await fetch(`/api/posts/${postId}/select-caption/${selectedCaptionIdx}`, {
          method: 'POST',
          credentials: 'include'
        });
        if (!selectRes.ok) throw new Error(await readApiError(selectRes, 'Failed to select caption'));
      }

      const scheduleRes = await fetch(`/api/posts/${postId}/schedule`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          scheduledAtUtc: scheduleAt,
          selectedCaptionIndex: selectedCaptionIdx ?? undefined,
          mediaIds: selectedMediaId ? [selectedMediaId] : []
        })
      });
      if (!scheduleRes.ok) throw new Error(await readApiError(scheduleRes, 'Failed to schedule post'));

      navigate('/posts');
    } catch (err) {
      setGlobalError(normalizeError(err, 'Unable to schedule post'));
    }
  };

  const captionCards = useMemo(() => captions.map((cap, idx) => (
    <div
      key={idx}
      className={`cp-card ${selectedCaptionIdx === idx ? 'selected' : ''}`}
      onClick={() => setSelectedCaptionIdx(idx)}
      tabIndex={0}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') setSelectedCaptionIdx(idx); }}
      role="button"
      aria-pressed={selectedCaptionIdx === idx}
    >
      <div className="cp-card-header">
        <span className="badge">Option {idx + 1}</span>
        {selectedCaptionIdx === idx && <span className="check">Chosen</span>}
      </div>
      <p>{cap}</p>
    </div>
  )), [captions, selectedCaptionIdx]);

  return (
    <div className="flow-shell animate-fade-in">
      <div className="flow-steps">
        <div className={`flow-step ${step === 'media' ? 'active' : ''}`}>Media</div>
        <div className={`flow-step ${step === 'content' ? 'active' : ''}`}>Content</div>
        <div className={`flow-step ${step === 'schedule' ? 'active' : ''}`}>Schedule</div>
      </div>

      {globalError && <div className="alert error">{globalError}</div>}

      {step === 'media' && (
        <div className="panel">
          <div className="panel-header">
            <h2>Select media</h2>
            <p>Pick an image or clip to pair with your post.</p>
          </div>
          <div className="media-grid">
            {media.length === 0 ? (
              <div className="empty-media">
                <div className="empty-icon"><Camera size={20} /></div>
                <p>No media yet. Upload in Media Library.</p>
              </div>
            ) : media.map(item => (
              <button
                key={item.id}
                className={`media-pick ${selectedMediaId === item.id ? 'active' : ''}`}
                onClick={() => setSelectedMediaId(item.id)}
                type="button"
              >
                <div className="thumb">
                  {item.type === 'video' ? <Film size={18} /> : <Camera size={18} />}
                </div>
                <span>{item.type}</span>
              </button>
            ))}
          </div>
          <div className="flow-actions">
            <button className="btn-primary" type="button" onClick={goNext} disabled={!canNextMedia}>Next</button>
          </div>
        </div>
      )}

      {step === 'content' && (
        <div className="panel">
          <div className="panel-header">
            <h2>Write & choose caption</h2>
            <p>Describe the post, generate options, then pick your favorite.</p>
          </div>
          <form className="form-grid" onSubmit={handleGenerate}>
            <label className="form-block">
              <span>Description</span>
              <textarea
                rows={4}
                required
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="What are you posting?"
              />
            </label>
            <div className="form-row">
              <label className="form-block">
                <span>Platform</span>
                <select value={platform} onChange={(e) => setPlatform(e.target.value)}>
                  <option value="instagram">Instagram</option>
                  <option value="tiktok">TikTok</option>
                  <option value="linkedin">LinkedIn</option>
                </select>
              </label>
              <label className="form-block">
                <span>Tone</span>
                <select value={tone} onChange={(e) => setTone(e.target.value)}>
                  <option value="professional">Professional</option>
                  <option value="funny">Funny</option>
                  <option value="inspirational">Inspirational</option>
                </select>
              </label>
              <label className="form-block">
                <span>Length</span>
                <select value={length} onChange={(e) => setLength(e.target.value)}>
                  <option value="short">Short</option>
                  <option value="medium">Medium</option>
                  <option value="long">Long</option>
                </select>
              </label>
            </div>
            {generateError && <div className="alert error">{generateError}</div>}
            <div className="flow-actions">
              <button type="button" className="btn-secondary" onClick={goPrev}><ArrowLeft size={14} /> Back</button>
              <button type="submit" className="btn-primary" disabled={isGenerating}>
                {isGenerating ? <><Loader2 className="spin" size={16} /> Generating…</> : <><Sparkles size={16} /> Generate</>}
              </button>
            </div>
          </form>

          {captions.length > 0 && (
            <div className="caption-grid">
              {captionCards}
            </div>
          )}

          <div className="flow-actions">
            <button className="btn-primary" type="button" onClick={goNext} disabled={!canNextContent}>
              Next
            </button>
          </div>
        </div>
      )}

      {step === 'schedule' && (
        <div className="panel">
          <div className="panel-header">
            <h2>Schedule</h2>
            <p>Pick when to publish and confirm.</p>
          </div>
          <div className="form-row">
            <label className="form-block">
              <span>Publish at (local)</span>
              <input
                type="datetime-local"
                value={scheduleAt}
                onChange={(e) => setScheduleAt(e.target.value)}
              />
            </label>
            <label className="form-block">
              <span>Selected caption</span>
              <div className="selected-caption-box">
                {selectedCaptionIdx !== null && captions[selectedCaptionIdx]
                  ? <><Tag size={14} /> {captions[selectedCaptionIdx]}</>
                  : 'Pick a caption first'}
              </div>
            </label>
          </div>
          {scheduleError && <div className="alert error">{scheduleError}</div>}
          <div className="flow-actions">
            <button className="btn-secondary" type="button" onClick={goPrev}><ArrowLeft size={14} /> Back</button>
            <button className="btn-primary" type="button" onClick={handlePost}>
              <BookmarkCheck size={16} /> Post
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
