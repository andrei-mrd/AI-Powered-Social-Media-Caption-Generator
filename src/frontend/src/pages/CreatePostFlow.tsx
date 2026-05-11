import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Camera, Film, ArrowLeft, Sparkles, BookmarkCheck, Tag, Loader2, Upload } from 'lucide-react';
import { readApiError, normalizeError } from '../utils/api';
import './CreatePostFlow.css';

type Step = 'media' | 'content' | 'schedule';

interface MediaItem {
  id: string;
  type: string;
  url: string;
  createdAtUtc: string;
}

interface CaptionVariant {
  variantIndex: number;
  text: string;
  hashtags: string[];
  hook?: string | null;
  cta?: string | null;
  score?: number | null;
}

interface CreateResponse {
  id: string;
  captions: CaptionVariant[];
  hashtags: string[];
  traceId?: string;
}

function SelectedCaptionPreview({ caption }: Readonly<{ caption?: CaptionVariant }>) {
  if (!caption) {
    return <>Pick a caption first</>;
  }

  return (
    <>
      <Tag size={14} /> {caption.text}
      {caption.hashtags?.length ? (
        <div className="selected-caption-hashtags">
          {caption.hashtags.map((hashtag) => <span key={hashtag} className="badge">{hashtag}</span>)}
        </div>
      ) : null}
    </>
  );
}

export default function CreatePostFlow() {
  const [step, setStep] = useState<Step>('media');
  const [media, setMedia] = useState<MediaItem[]>([]);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState('');
  const [selectedMediaId, setSelectedMediaId] = useState<string | null>(null);
  const [description, setDescription] = useState('');
  const [platform, setPlatform] = useState('instagram');
  const [tone, setTone] = useState('professional');
  const [length, setLength] = useState('medium');
  const [language, setLanguage] = useState('en');
  const [goal, setGoal] = useState('awareness');
  const [includeEmojis, setIncludeEmojis] = useState(true);
  const [includeCta, setIncludeCta] = useState(true);
  const [hashtagCount, setHashtagCount] = useState(8);
  const [audience, setAudience] = useState('');
  const [brandVoice, setBrandVoice] = useState('');
  const [keywordsToInclude, setKeywordsToInclude] = useState('');
  const [forbiddenWords, setForbiddenWords] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [generateError, setGenerateError] = useState('');
  const [captions, setCaptions] = useState<CaptionVariant[]>([]);
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

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    if (!event.target.files || event.target.files.length === 0) return;
    const file = event.target.files[0];
    setUploading(true);
    setUploadError('');
    try {
      const form = new FormData();
      form.append('file', file);
      const res = await fetch('/api/media/upload', {
        method: 'POST',
        credentials: 'include',
        body: form
      });
      if (!res.ok) throw new Error(await readApiError(res, 'Upload failed'));
      const data = await res.json();
      setMedia(prev => [data, ...prev]);
      setSelectedMediaId(data.id);
    } catch (err) {
      setUploadError(normalizeError(err, 'Upload failed'));
    } finally {
      setUploading(false);
      event.target.value = '';
    }
  };

  const canNextContent = step === 'content' ? !!postId && selectedCaptionIdx !== null : true;

  const goNext = () => {
    if (step === 'media') setStep('content');
    else if (step === 'content' && canNextContent) setStep('schedule');
  };

  const goPrev = () => {
    if (step === 'schedule') setStep('content');
    else if (step === 'content') setStep('media');
  };

  const handleGenerate = async (e: FormEvent<HTMLFormElement>) => {
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
          language: language || 'en',
          goal: goal || 'awareness',
          captionLength: length.toLowerCase(),
          includeEmojis,
          includeCta,
          hashtagCount: hashtagCount || 8,
          audience: audience || null,
          brandVoice: brandVoice || null,
          keywordsToInclude: keywordsToInclude
            .split(',')
            .map(k => k.trim())
            .filter(Boolean),
          forbiddenWords: forbiddenWords
            .split(',')
            .map(k => k.trim())
            .filter(Boolean),
          count: 3
        })
      });
      if (!res.ok) throw new Error(await readApiError(res, 'Generation failed'));
      const data: CreateResponse = await res.json();
      setPostId(data.id);
      setCaptions(data.captions ?? []);
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
    <button
      key={cap.variantIndex}
      type="button"
      className={`cp-card ${selectedCaptionIdx === idx ? 'selected' : ''}`}
      onClick={() => setSelectedCaptionIdx(idx)}
      aria-pressed={selectedCaptionIdx === idx}
    >
      <div className="cp-card-header">
        <span className="badge">Option {idx + 1}</span>
        {selectedCaptionIdx === idx && <span className="check">Chosen</span>}
      </div>
      <p>{cap.text}</p>
      <div className="card-meta">
        {cap.score ? `Score ${cap.score}` : 'Tap to choose'} • {cap.hashtags?.slice(0, 4).join(' ')}
      </div>
    </button>
  )), [captions, selectedCaptionIdx]);

  return (
    <div className="flow-shell animate-fade-in">
      <div className="flow-hero">
        <div>
          <p className="eyebrow">Create & ship</p>
          <h1>Build a post in three playful steps</h1>
          <p className="muted">Pick media, generate captions, and lock in your go-live time.</p>
        </div>
        <div className="orbit">
          <span className={step === 'media' ? 'dot active' : 'dot'} />
          <span className={step === 'content' ? 'dot active' : 'dot'} />
          <span className={step === 'schedule' ? 'dot active' : 'dot'} />
        </div>
      </div>

      <div className="flow-steps">
        <div className={`flow-step ${step === 'media' ? 'active' : ''}`}>Media</div>
        <div className={`flow-step ${step === 'content' ? 'active' : ''}`}>Caption</div>
        <div className={`flow-step ${step === 'schedule' ? 'active' : ''}`}>Schedule</div>
      </div>

      {globalError && <div className="alert error">{globalError}</div>}

      {step === 'media' && (
        <div className="panel">
          <div className="panel-header">
            <h2>Select media</h2>
            <p>Pick an image or clip to pair with your post.</p>
          </div>
          <div className="upload-inline">
            <label className="upload-btn">
              <Upload size={16} /> {uploading ? 'Uploading…' : 'Upload media'}
              <input type="file" accept="image/png,image/jpeg,image/webp,video/mp4,video/quicktime" onChange={handleUpload} disabled={uploading} />
            </label>
            {uploadError && <div className="alert error">{uploadError}</div>}
            <p className="helper">Allowed: jpg, png, webp, mp4, mov. Max 20MB.</p>
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
            <button className="btn-primary" type="button" onClick={goNext}>Next</button>
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
            <div className="form-row">
              <label className="form-block">
                <span>Language</span>
                <select value={language} onChange={(e) => setLanguage(e.target.value)}>
                  <option value="en">English</option>
                  <option value="ro">Română</option>
                </select>
              </label>
              <label className="form-block">
                <span>Goal</span>
                <select value={goal} onChange={(e) => setGoal(e.target.value)}>
                  <option value="awareness">Awareness</option>
                  <option value="engagement">Engagement</option>
                  <option value="sales">Sales</option>
                </select>
              </label>
              <label className="form-block">
                <span>Hashtags (count)</span>
                <input
                  type="number"
                  min={5}
                  max={20}
                  value={hashtagCount}
                  onChange={(e) => setHashtagCount(Number(e.target.value))}
                />
              </label>
            </div>
            <div className="form-row">
              <label className="form-block checkbox-row">
                <input
                  type="checkbox"
                  checked={includeEmojis}
                  onChange={(e) => setIncludeEmojis(e.target.checked)}
                />
                <span>Include emojis</span>
              </label>
              <label className="form-block checkbox-row">
                <input
                  type="checkbox"
                  checked={includeCta}
                  onChange={(e) => setIncludeCta(e.target.checked)}
                />
                <span>Include CTA</span>
              </label>
              <label className="form-block">
                <span>Audience</span>
                <input
                  type="text"
                  value={audience}
                  onChange={(e) => setAudience(e.target.value)}
                  placeholder="e.g. eco-conscious buyers"
                />
              </label>
            </div>
            <div className="form-row">
              <label className="form-block">
                <span>Brand voice</span>
                <input
                  type="text"
                  value={brandVoice}
                  onChange={(e) => setBrandVoice(e.target.value)}
                  placeholder="e.g. witty, concise"
                />
              </label>
              <label className="form-block">
                <span>Keywords to include (comma-separated)</span>
                <textarea
                  rows={2}
                  value={keywordsToInclude}
                  onChange={(e) => setKeywordsToInclude(e.target.value)}
                  placeholder="eco, packaging, recycled"
                />
              </label>
              <label className="form-block">
                <span>Forbidden words (comma-separated)</span>
                <textarea
                  rows={2}
                  value={forbiddenWords}
                  onChange={(e) => setForbiddenWords(e.target.value)}
                  placeholder="cheap, discount"
                />
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
                <SelectedCaptionPreview caption={selectedCaptionIdx !== null ? captions[selectedCaptionIdx] : undefined} />
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
