import { Link } from 'react-router-dom';
import { PenTool, Target, Hash, Zap } from 'lucide-react';
import './Dashboard.css';

export default function Dashboard() {
  return (
    <div className="dashboard-container animate-fade-in">
      <header className="page-header">
        <div>
          <h1 className="page-title">Dashboard</h1>
          <p className="page-subtitle">Ship captions without losing your voice</p>
        </div>
      </header>

      <div className="bento-grid">
        <div className="bento-item welcome-box animate-slide-up">
          <span className="eyebrow">Welcome back</span>
          <h2>Keep posts feeling human</h2>
          <p>Rotate tones, test hooks, and keep captions concise without sounding generic.</p>
          <div className="action-row">
            <Link to="/generate" className="btn-primary">
              <PenTool size={18} /> Start generating
            </Link>
            <button className="ghost-btn">Template Library</button>
          </div>
        </div>

        <div className="bento-item features-box animate-slide-up animate-delay-100">
          <div className="feat-row">
            <div className="feat-icon-small"><Target size={16}/></div>
            <div className="feat-text">
              <strong>Tones</strong>
              <span>Presets for every mood</span>
            </div>
          </div>
          <div className="feat-row">
            <div className="feat-icon-small"><Zap size={16}/></div>
            <div className="feat-text">
              <strong>Length</strong>
              <span>Auto-optimized limits</span>
            </div>
          </div>
          <div className="feat-row">
            <div className="feat-icon-small"><Hash size={16}/></div>
            <div className="feat-text">
              <strong>Tags</strong>
              <span>Smart hashtag clusters</span>
            </div>
          </div>
        </div>

        <div className="bento-item info-box animate-slide-up animate-delay-200">
          <h4>Need inspiration?</h4>
          <p>Try giving us a rough idea, product name, or vibe—we will return 3 distinct angles with hashtags instantly.</p>
        </div>
        
        <div className="bento-item highlight-box animate-slide-up animate-delay-300">
          <h4>Plan a series</h4>
          <p>Save prompt snippets for campaigns so every post stays aligned across platforms.</p>
        </div>
      </div>
    </div>
  );
}
