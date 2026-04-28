import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Sparkles, Hash, Copy, CheckCircle2, Bookmark } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { readApiError, normalizeError } from '../utils/api';
import './Generator.css';

const containerVariants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { staggerChildren: 0.1 } }
};

const itemVariants = {
  hidden: { opacity: 0, y: 15 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.4 } }
};

interface CaptionVariant {
  text: string;
  hashtags: string[];
}

interface GenerateResult {
  id: string;
  captions: CaptionVariant[];
  hashtags: string[];
}

export default function Generator() {
  const [topic, setTopic] = useState('');
  const [platform, setPlatform] = useState('instagram');
  const [tone, setTone] = useState('professional');
  const [length, setLength] = useState('medium');
  const [isGenerating, setIsGenerating] = useState(false);
  const [result, setResult] = useState<GenerateResult | null>(null);
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

      const data = (await res.json()) as GenerateResult;
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

  const handleSaveSelected = () => {
    if (selectedIndex === null) return;
    setSavedIndex(selectedIndex);
    setTimeout(() => navigate('/posts'), 1200);
  };

  const handleSelectCard = (index: number) => {
    setSelectedIndex(index);
    setSavedIndex(null);
  };

  return (
    <motion.div 
      className="generator-layout"
      initial="hidden"
      animate="visible"
      variants={containerVariants}
    >
      <motion.div variants={itemVariants} className="config-panel glass-panel">
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
              rows={5}
              placeholder="e.g. Launching our new summer collection next week entirely focused on premium sustainable cotton..."
              value={topic}
              onChange={(e) => setTopic(e.target.value)}
              className="resize-none generator-textarea"
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
                  <option value="twitter">Twitter / X</option>
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
                  <option value="punchy">Punchy</option>
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

          <AnimatePresence>
            {error && (
              <motion.div 
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                className="alert error mt-4"
              >
                {error}
              </motion.div>
            )}
          </AnimatePresence>

          <div className="form-footer mt-auto pt-6">
            <button type="submit" className="btn btn-primary w-full btn-generate" disabled={isGenerating || !topic}>
              {isGenerating ? (
                <><div className="pulse-loader"></div> Crafting magic...</>
              ) : (
                <><Sparkles size={18} /> Generate Captions</>
              )}
            </button>
          </div>
        </form>
      </motion.div>

      <motion.div variants={itemVariants} className="preview-panel">
        <AnimatePresence mode="wait">
          {!isGenerating && !result && (
            <motion.div 
              key="empty"
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              className="empty-canvas glass-panel"
            >
              <div className="welcome-visual-circle">
                <Sparkles size={32} className="text-indigo-500" />
              </div>
              <h3>Your canvas is ready</h3>
              <p>Fill out the configuration on the left and hit generate to see your results.</p>
            </motion.div>
          )}

          {isGenerating && (
            <motion.div 
              key="generating"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="empty-canvas glass-panel generating-state"
            >
              <div className="loader-container">
                <div className="spinner-ring"></div>
                <div className="spinner-center"></div>
              </div>
              <h3>Analyzing context</h3>
              <p>Our AI is writing multiple variations based on best practices for {platform}...</p>
            </motion.div>
          )}

          {result && !isGenerating && (
            <motion.div 
              key="results"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="results-view"
            >
              <div className="results-header">
                <div className="results-title">
                  <h2>Generated Options</h2>
                  <span className="badge badge-subtle">{platform}</span>
                </div>
                <button
                  type="button"
                  className={`btn ${savedIndex !== null ? 'btn-secondary text-green-600' : 'btn-primary'}`}
                  onClick={handleSaveSelected}
                  disabled={selectedIndex === null || savedIndex !== null}
                >
                  {savedIndex !== null ? <CheckCircle2 size={16} /> : <Bookmark size={16} />}
                  {savedIndex !== null ? 'Saved to Library' : 'Save Selected'}
                </button>
              </div>

              <div className="options-list">
                {result.captions.map((cap, i) => (
                  <motion.div
                    initial={{ opacity: 0, x: -20 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: i * 0.1 }}
                    key={i}
                    className={`result-card ${selectedIndex === i ? 'selected' : ''}`}
                    onClick={() => handleSelectCard(i)}
                  >
                    <div className="card-top">
                      <span className="option-label">Option {i + 1}</span>
                      <div className="card-actions">
                        {selectedIndex === i && (
                          <span className="selected-pill">
                            <span className="selected-dot"></span> Selected
                          </span>
                        )}
                        <button
                          type="button"
                          className="copy-btn" 
                          onClick={(e) => { e.stopPropagation(); copyToClipboard(cap.text, i); }}
                        >
                          {copiedIndex === i ? <CheckCircle2 size={16} className="text-green-500" /> : <Copy size={16} />}
                        </button>
                      </div>
                    </div>
                    <div className="card-body">
                      {cap.text}
                    </div>
                  </motion.div>
                ))}
              </div>

              {result.hashtags && result.hashtags.length > 0 && (
                <motion.div 
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ delay: 0.5 }}
                  className="hashtags-box glass-panel"
                >
                  <div className="hash-header">
                    <Hash size={16} className="text-indigo-500" />
                    <h4 className="hashtags-title">Recommended Tags</h4>
                  </div>
                  <div className="tags-list">
                    {result.hashtags.map((tag: string, i: number) => (
                      <span key={i} className="hashtag">{tag}</span>
                    ))}
                  </div>
                </motion.div>
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>
    </motion.div>
  );
}
