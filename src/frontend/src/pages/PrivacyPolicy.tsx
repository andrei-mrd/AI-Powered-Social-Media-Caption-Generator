import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import './LegalPage.css'

export default function PrivacyPolicy() {
  return (
    <div className="legal-wrapper">
      <nav className="legal-nav">
        <Link to="/" className="legal-brand">
          <div className="brand-dot" />
          CaptionGen
        </Link>
        <Link to="/" className="legal-nav-back">
          <ArrowLeft size={16} />
          Back to home
        </Link>
      </nav>

      <main className="legal-main">
        <div className="legal-header">
          <div className="legal-badge">Legal</div>
          <h1>Privacy Policy</h1>
          <p className="legal-meta">Last updated: April 21, 2026</p>
        </div>

        <div className="legal-body">
          <div className="legal-section">
            <h2>1. Overview</h2>
            <p>
              CaptionGen ("we", "us", or "our") is committed to protecting your privacy. This Privacy
              Policy explains how we collect, use, and safeguard your information when you use our
              Service. By using CaptionGen, you agree to the practices described in this policy.
            </p>
          </div>

          <div className="legal-section">
            <h2>2. Information We Collect</h2>
            <p>We collect the following types of information:</p>
            <ul>
              <li>
                <strong>Account information:</strong> your email address and hashed password when you
                register
              </li>
              <li>
                <strong>Usage data:</strong> captions generated, posts created, credits used, and
                feature interactions
              </li>
              <li>
                <strong>Content you provide:</strong> descriptions, media URLs, brand voice settings,
                and other inputs used to generate captions
              </li>
              <li>
                <strong>Payment data:</strong> processed securely through Stripe; we do not store
                your card details
              </li>
              <li>
                <strong>Social account tokens:</strong> OAuth tokens for connected accounts (e.g.,
                LinkedIn), stored encrypted
              </li>
              <li>
                <strong>Technical data:</strong> IP address, browser type, request trace IDs, and
                error logs for debugging
              </li>
            </ul>
          </div>

          <div className="legal-section">
            <h2>3. How We Use Your Information</h2>
            <p>We use the information we collect to:</p>
            <ul>
              <li>Provide and improve the caption generation Service</li>
              <li>Manage your account and authenticate your identity</li>
              <li>Process payments and manage your credits and subscription</li>
              <li>Publish posts to connected social media accounts on your behalf</li>
              <li>Detect and prevent abuse, fraud, and policy violations</li>
              <li>Send transactional emails (e.g., password reset, billing confirmation)</li>
              <li>Comply with legal obligations</li>
            </ul>
          </div>

          <div className="legal-section">
            <h2>4. AI and Content Processing</h2>
            <p>
              When you generate captions, the description and settings you provide are sent to our
              AI service (powered by OpenAI) to produce the output. Your inputs may also be passed
              through a content moderation API to detect unsafe content. We do not use your inputs
              to train AI models.
            </p>
          </div>

          <div className="legal-section">
            <h2>5. Third-Party Integrations</h2>
            <p>
              CaptionGen integrates with third-party services, each with their own privacy policies:
            </p>
            <ul>
              <li>
                <strong>OpenAI</strong> — used for caption generation and content moderation
              </li>
              <li>
                <strong>Stripe</strong> — used for payment processing
              </li>
              <li>
                <strong>LinkedIn, TikTok, and other social platforms</strong> — used when you
                connect your social accounts for publishing
              </li>
            </ul>
            <p>
              When you connect a social media account, we store the OAuth access token in encrypted
              form solely to publish content on your behalf. We never post without your explicit action.
            </p>
          </div>

          <div className="legal-section">
            <h2>6. Data Sharing</h2>
            <p>
              We do not sell your personal data. We may share information with:
            </p>
            <ul>
              <li>Third-party service providers listed above, under strict data processing agreements</li>
              <li>Law enforcement or regulatory authorities when required by law</li>
              <li>Successors in the event of a merger or acquisition, with advance notice to you</li>
            </ul>
          </div>

          <div className="legal-section">
            <h2>7. Data Retention</h2>
            <p>
              We retain your account data for as long as your account is active. Generated captions
              and posts are stored to power your history and analytics features. You may request
              deletion of your account and associated data at any time.
            </p>
          </div>

          <div className="legal-section">
            <h2>8. Security</h2>
            <p>
              We use industry-standard measures to protect your data, including encrypted storage
              of sensitive tokens, HTTPS for all data in transit, and hashed password storage. No
              system is completely secure, and we cannot guarantee absolute security.
            </p>
          </div>

          <div className="legal-section">
            <h2>9. Your Rights</h2>
            <p>Depending on your location, you may have the right to:</p>
            <ul>
              <li>Access the personal data we hold about you</li>
              <li>Correct inaccurate data</li>
              <li>Request deletion of your data</li>
              <li>Object to or restrict certain processing</li>
              <li>Port your data to another service</li>
            </ul>
            <p>
              To exercise any of these rights, contact us at the address below.
            </p>
          </div>

          <div className="legal-section">
            <h2>10. Cookies</h2>
            <p>
              CaptionGen uses minimal browser storage (localStorage) to persist your authentication
              token locally on your device. We do not use third-party tracking cookies or
              advertising cookies.
            </p>
          </div>

          <div className="legal-section">
            <h2>11. Children's Privacy</h2>
            <p>
              CaptionGen is not directed to individuals under the age of 13. We do not knowingly
              collect personal data from children. If you believe a child has provided us with
              personal information, please contact us immediately.
            </p>
          </div>

          <div className="legal-section">
            <h2>12. Changes to This Policy</h2>
            <p>
              We may update this Privacy Policy periodically. We will notify you of significant
              changes via email or an in-app notice. Your continued use of the Service after
              changes take effect constitutes your acceptance of the updated policy.
            </p>
          </div>

          <div className="legal-section">
            <h2>13. Contact</h2>
            <div className="legal-contact-box">
              <p>
                For privacy-related questions or to exercise your rights, contact us:
              </p>
              <p>
                <strong>Email:</strong>{' '}
                <a href="mailto:andreidanielmardare@gmail.com">andreidanielmardare@gmail.com</a>
              </p>
            </div>
          </div>
        </div>
      </main>

      <footer className="legal-footer">
        © {new Date().getFullYear()} CaptionGen. All rights reserved. ·{' '}
        <Link to="/terms" style={{ color: 'var(--accent-indigo)', textDecoration: 'none' }}>
          Terms of Service
        </Link>
      </footer>
    </div>
  )
}
