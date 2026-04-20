import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowRight, Sparkles, Zap, Layers, BarChart3, Edit3 } from 'lucide-react';
import './HomePage.css';

const fadeIn = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.6, ease: "easeOut" } }
};

const staggerContainer = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.15
    }
  }
};

export default function HomePage() {
  return (
    <div className="home-wrapper">
      <header className="home-nav glass-panel">
        <div className="home-brand">
          <div className="brand-dot" />
          <span className="text-gradient">CaptionGen</span>
        </div>
        <div className="home-nav-actions">
          <Link to="/login" className="btn-ghost">Sign In</Link>
          <Link to="/register" className="btn-primary">Get Started</Link>
        </div>
      </header>

      <main className="home-main">
        <motion.section 
          className="hero-section"
          initial="hidden"
          animate="visible"
          variants={staggerContainer}
        >
          <div className="hero-content">
            <motion.div variants={fadeIn} className="badge badge-subtle">
              <Sparkles size={14} />
              <span>AI-Powered Caption Lab</span>
            </motion.div>
            
            <motion.h1 variants={fadeIn} className="hero-title">
              Make every post <br/> sound <span className="text-gradient">intentional.</span>
            </motion.h1>
            
            <motion.p variants={fadeIn} className="hero-lead">
              Transform your ideas into clean, engaging, on-brand copy for any platform without the overthinking. Powered by advanced AI.
            </motion.p>

            <motion.div variants={fadeIn} className="cta-row">
              <Link to="/register" className="btn-primary btn-large">
                Start Creating Free <ArrowRight size={18} />
              </Link>
              <Link to="/login" className="btn-secondary btn-large">
                Login to Dashboard
              </Link>
            </motion.div>
            
            <motion.div variants={fadeIn} className="social-proof">
              <div className="avatars">
                <div className="avatar"></div>
                <div className="avatar"></div>
                <div className="avatar"></div>
                <div className="avatar"></div>
              </div>
              <span>Trusted by <strong>10,000+</strong> content creators</span>
            </motion.div>
          </div>

          <motion.div variants={fadeIn} className="hero-visual animate-float">
            <div className="glass-mockup">
              <div className="mockup-header">
                <div className="mac-dots">
                  <span className="red" /> <span className="yellow" /> <span className="green" />
                </div>
                <div className="mockup-title">caption_gen_studio.app</div>
              </div>
              <div className="mockup-body">
                <div className="mock-input-group">
                  <div className="mock-label">Describe your post</div>
                  <div className="mock-input">Just launched our new minimalist desk setup drop.</div>
                </div>
                
                <div className="mock-row">
                  <div className="mock-tag blue"><Sparkles size={12}/> Professional</div>
                  <div className="mock-tag purple"><Zap size={12}/> Engaging</div>
                </div>
                
                <div className="mock-result">
                  <div className="result-header">
                    <Layers size={14} className="text-indigo-500" />
                    <span>Generated Options</span>
                  </div>
                  <p>
                    "Elevate your workspace. 🌬️ The new Minimalist Desk Collection is officially live. Clean lines, zero clutter, and designed for deep work. Tap the link in bio to explore the drop."
                  </p>
                  <div className="hashtags">#DeskSetup #MinimalVibes #DeepWork</div>
                </div>
              </div>
            </div>
          </motion.div>
        </motion.section>

        <motion.section 
          className="features-bento"
          initial="hidden"
          whileInView="visible"
          viewport={{ once: true, margin: "-100px" }}
          variants={staggerContainer}
        >
          <motion.div variants={fadeIn} className="bento-header">
            <h2>Everything you need for viral content</h2>
            <p>Smart tools designed for creators and marketing teams.</p>
          </motion.div>

          <div className="bento-grid">
            <motion.div variants={fadeIn} className="bento-card span-2 feature-card highlight-card">
              <div className="feat-icon blue"><Layers size={24}/></div>
              <h3>Platform Aware Generation</h3>
              <p>Automatically adapt your copy lengths, formatting, and tone for LinkedIn, Twitter, Instagram, or TikTok natively.</p>
              <div className="bento-image platform-img"></div>
            </motion.div>
            
            <motion.div variants={fadeIn} className="bento-card feature-card">
              <div className="feat-icon green"><Zap size={24}/></div>
              <h3>Hashtag Intelligence</h3>
              <p>Pair niche tags with hero keywords dynamically so your posts gain organic reach without looking spammy.</p>
            </motion.div>
            
            <motion.div variants={fadeIn} className="bento-card feature-card">
              <div className="feat-icon purple"><BarChart3 size={24}/></div>
              <h3>Analytics Ready</h3>
              <p>Craft captions optimized for conversion and clicks based on proven historical engagement data.</p>
            </motion.div>
            
            <motion.div variants={fadeIn} className="bento-card span-2 feature-card dark-card">
              <div className="feat-icon white"><Edit3 size={24}/></div>
              <h3>Team-Friendly Prompts</h3>
              <p>Share your best performing prompts and maintain brand tone consistency across your entire marketing squad.</p>
            </motion.div>
          </div>
        </motion.section>
      </main>
    </div>
  );
}
