import { useState } from 'react';
import { Sparkles, Hash, Copy, CheckCircle2, Bookmark } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { readApiError, normalizeError } from '../utils/api';
import './Generator.css';

export default function Generator() {
  const [topic, setTopic] = useState('');
  const [platform, setPlatform] = useState('instagram');
  const [tone, setTone] = useState('professional');
  const [length, setLength] = useState('medium');
  const [isGenerating, setIsGenerating] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState('');
  const [copiedIndex, setCopiedIndex] = useState<number | null>(null);
  const [savedIndex, setSavedIndex] = useState<number | null>(null);
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const navigate = useNavigate();

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!topic) return;
    
    setIsGenerating(true);
    setError('');
    setResult(null);
    setSavedIndex(null);
    setSelectedIndex(null);

    try {
      // Assumes Vite proxy forwards /api to backend
      const res = await fetch('/api/posts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({
          description: topic,
          platform: platform,
          tone: tone.toLowerCase(),
          language: 'en',
          goal: 'awareness',
          captionLength: length.toLowerCase(),
          includeEmojis: true,
          includeCta: true,
          hashtagCount: 5,
          count: 3
        })
      });

      if (!res.ok) {
        const message = await readApiError(res, 'Failed to generate');
        throw new Error(message);
      }

      const data = await res.json();
      setResult(data);
      setSelectedIndex(data?.captions?.length ? 0 : null);
      setSavedIndex(null);
    } catch (err) {
      setError(normalizeError(err, 'Unable to generate captions'));
    } finally {
      setIsGenerating(false);
    }
  };

  const copyToClipboard = (text: string, index: number) => {
    navigator.clipboard.writeText(text);
    setCopiedIndex(index);
    setTimeout(() => setCopiedIndex(null), 2000);
  };

  // The post is already persisted on generate. The Save button marks which
  // variant the user prefers and navigates them to My Posts.
  const handleSaveSelected = () => {
    if (selectedIndex === null) return;
    setSavedIndex(selectedIndex);
    setTimeout(() => navigate('/posts'), 1200);
  };

  const handleSelectCard = (index: number) => {
    setSelectedIndex(index);
    setSavedIndex(null);
  };

  const handleCardKeyDown = (event: React.KeyboardEvent<HTMLDivElement>, index: number) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      handleSelectCard(index);
    }
  };

  return (
    <div className="generator-layout animate-fade-in">
      <div className="config-panel">
        <div className="panel-header">
          <h1 className="panel-title">Create Post</h1>
          <p className="panel-subtitle">Configure your narrative</p>
        </div>

        <form onSubmit={handleGenerate} className="config-form">
          <div className="form-group mb-4">
            <label htmlFor="topic">What are we posting about?</label>
            <p className="help-text">Give us a rough idea, product name, or key message.</p>
            <textarea 
              id="topic" 
              rows={4}
              placeholder="e.g. Launching our new summer collection next week entirely focused on premium sustainable cotton..."
              value={topic}
              onChange={(e) => setTopic(e.target.value)}
              className="resize-none"
              required
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="platform">Platform</label>
              <div className="select-wrapper">
                <select id="platform" value={platform} onChange={(e) => setPlatform(e.target.value)}>
                  <option value="instagram">Instagram</option>
                  <option value="tiktok">TikTok</option>
                  <option value="linkedin">LinkedIn</option>
                </select>
              </div>
            </div>
            <div className="form-group">
              <label htmlFor="tone">Tone of voice</label>
              <div className="select-wrapper">
                <select id="tone" value={tone} onChange={(e) => setTone(e.target.value)}>
                  <option value="professional">Professional</option>
                  <option value="funny">Funny</option>
                  <option value="inspirational">Inspirational</option>
                </select>
              </div>
            </div>
          </div>

          <div className="form-group mt-4">
            <label htmlFor="length">Length Preference</label>
            <div className="select-wrapper">
              <select id="length" value={length} onChange={(e) => setLength(e.target.value)}>
                <option value="short">Short</option>
                <option value="medium">Medium</option>
                <option value="long">Long</option>
              </select>
            </div>
          </div>

          {error && <div className="alert error mt-4">{error}</div>}

          <div className="form-footer mt-auto pt-6 border-t border-subtle">
            <button type="submit" className="btn-generate" disabled={isGenerating || !topic}>
              {isGenerating ? (
                <><div className="pulse-loader"></div> Crafting...</>
              ) : (
                <><Sparkles size={18} /> Generate Captions</>
              )}
            </button>
          </div>
        </form>
      </div>

      <div className="preview-panel">
        {!isGenerating && !result && (
          <div className="empty-canvas">
            <div className="empty-icon-box">
              <Sparkles size={24} className="text-gray-400" />
            </div>
            <h3>Your canvas is ready</h3>
            <p>Fill out the configuration on the left and hit generate to see your results.</p>
          </div>
        )}

        {isGenerating && (
          <div className="empty-canvas">
            <div className="pulse-loader large mb-4"></div>
            <h3>Analyzing context</h3>
            <p>Our AI is writing multiple variations for {platform}...</p>
          </div>
        )}

        {result && !isGenerating && (
          <div className="results-view animate-slide-up">
            <div className="results-header">
              <div className="results-title">
                <h2>Generated Options</h2>
                <span className="badge">{result.platform}</span>
              </div>
              <button
                type="button"
                className={`save-btn save-all-btn ${savedIndex !== null ? 'saved' : ''}`}
                onClick={handleSaveSelected}
                disabled={selectedIndex === null || savedIndex !== null}
              >
                {savedIndex !== null ? <CheckCircle2 size={16} className="text-green-500" /> : <Bookmark size={16} />}
                {savedIndex !== null ? 'Saved to My Posts' : 'Save Selected'}
              </button>
            </div>

            <div className="options-list">
              {result.captions.map((cap: string, i: number) => (
                <div
                  key={i}
                  className={`result-card ${selectedIndex === i ? 'selected' : ''}`}
                  onClick={() => handleSelectCard(i)}
                  onKeyDown={(e) => handleCardKeyDown(e, i)}
                  tabIndex={0}
                  role="button"
                  aria-pressed={selectedIndex === i}
                >
                  <div className="card-top">
                    <span className="option-label">Option {i + 1}</span>
                    <div className="card-actions">
                      {selectedIndex === i && (
                        <span className="selected-pill">
                          <span className="selected-dot" aria-hidden="true"></span>
                          Selected
                        </span>
                      )}
                      <button
                        type="button"
                        className="copy-btn" 
                        onClick={(e) => { e.stopPropagation(); copyToClipboard(cap, i); }}
                        aria-label="Copy to clipboard"
                      >
                        {copiedIndex === i ? <CheckCircle2 size={16} className="text-green-500" /> : <Copy size={16} />}
                        {copiedIndex === i ? 'Copied' : 'Copy'}
                      </button>
                    </div>
                  </div>
                  <div className="card-body">
                    {cap}
                  </div>
                </div>
              ))}
            </div>

            {result.hashtags && result.hashtags.length > 0 && (
              <div className="hashtags-box">
                <div className="hash-header">
                  <Hash size={16} className="text-gray-500" />
                  <h4 className="hashtags-title">Recommended Tags</h4>
                </div>
                <div className="tags-list">
                  {result.hashtags.map((tag: string, i: number) => (
                    <span key={i} className="hashtag">{tag}</span>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
