import { Link } from 'react-router-dom';
import { ArrowRight, Sparkles, Zap, Layers } from 'lucide-react';
import './HomePage.css';

export default function HomePage() {
  return (
    <div className="home-wrapper">
      <header className="home-nav">
        <div className="home-brand">
          <div className="brand-dot" />
          <span>CaptionGen</span>
        </div>
        <div className="home-nav-actions">
          <Link to="/login" className="btn-ghost">Sign in</Link>
          <Link to="/register" className="btn-dark">Get started</Link>
        </div>
      </header>

      <main className="home-main">
        <section className="hero-section animate-fade-in">
          <div className="hero-content">
            <div className="badge-pill animate-slide-up">
              <Sparkles size={14} className="text-blue-500" />
              <span>AI-Powered Caption Lab</span>
            </div>
            <h1 className="animate-slide-up animate-delay-100">
              Make every post sound intentional.
            </h1>
            <p className="hero-lead animate-slide-up animate-delay-200">
              CaptionGen turns your ideas into clean, on-brand copy for every platform without the overthinking.
            </p>

            <div className="cta-row animate-slide-up animate-delay-300">
              <Link to="/register" className="btn-primary">
                Start for free <ArrowRight size={18} />
              </Link>
              <Link to="/login" className="btn-secondary">
                Login to account
              </Link>
            </div>
          </div>

          <div className="hero-visual animate-fade-in animate-delay-300">
            <div className="abstract-card">
              <div className="card-header">
                <div className="mac-dots">
                  <span /> <span /> <span />
                </div>
              </div>
              <div className="card-body">
                <div className="mock-row">
                  <span className="mock-label">Tone</span>
                  <span className="mock-tag blue">Bold</span>
                  <span className="mock-tag green">Playful</span>
                </div>
                <div className="mock-caption">
                  "Launching the drop your feed's been waiting for. Crisp visuals, clean lines, and captions that sound like you."
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className="features-grid">
          <div className="feature-card">
            <div className="feat-icon blue"><Layers size={20}/></div>
            <h3>Platform aware</h3>
            <p>Adapts your copy for the feed you're posting to, maintaining platform-native limits.</p>
          </div>
          <div className="feature-card">
            <div className="feat-icon green"><Zap size={20}/></div>
            <h3>Hashtag intelligence</h3>
            <p>Pairs niche tags with hero keywords so posts get discovered without spam.</p>
          </div>
          <div className="feature-card">
            <div className="feat-icon purple"><Sparkles size={20}/></div>
            <h3>Team-friendly</h3>
            <p>Share prompts, keep tone presets, perfect for marketing squads.</p>
          </div>
        </section>
      </main>
    </div>
  );
}
