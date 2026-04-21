import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import './LegalPage.css'

export default function TermsOfService() {
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
          <h1>Terms of Service</h1>
          <p className="legal-meta">Last updated: April 21, 2026</p>
        </div>

        <div className="legal-body">
          <div className="legal-section">
            <h2>1. Acceptance of Terms</h2>
            <p>
              By accessing or using CaptionGen ("the Service"), you agree to be bound by these Terms of
              Service. If you do not agree to these terms, please do not use the Service.
            </p>
          </div>

          <div className="legal-section">
            <h2>2. Description of Service</h2>
            <p>
              CaptionGen is an AI-powered social media caption generator that helps users create, improve,
              and schedule captions for platforms including Instagram, TikTok, and LinkedIn. The Service
              uses artificial intelligence to generate content based on user-provided descriptions and preferences.
            </p>
          </div>

          <div className="legal-section">
            <h2>3. User Accounts</h2>
            <p>
              To access certain features, you must create an account. You are responsible for:
            </p>
            <ul>
              <li>Maintaining the confidentiality of your account credentials</li>
              <li>All activity that occurs under your account</li>
              <li>Notifying us immediately of any unauthorized use of your account</li>
              <li>Providing accurate and up-to-date information during registration</li>
            </ul>
          </div>

          <div className="legal-section">
            <h2>4. Acceptable Use</h2>
            <p>You agree not to use the Service to:</p>
            <ul>
              <li>Generate content that is illegal, harmful, threatening, abusive, or defamatory</li>
              <li>Violate the terms of service of any third-party platform (including TikTok, Instagram, or LinkedIn)</li>
              <li>Infringe on the intellectual property rights of others</li>
              <li>Attempt to reverse-engineer, hack, or disrupt the Service</li>
              <li>Use automated means to access the Service in a manner that exceeds normal usage</li>
            </ul>
          </div>

          <div className="legal-section">
            <h2>5. AI-Generated Content</h2>
            <p>
              The Service uses AI models to generate captions. You acknowledge that:
            </p>
            <ul>
              <li>AI-generated content may not always be accurate, appropriate, or fit for your purpose</li>
              <li>You are solely responsible for reviewing and approving any content before publishing</li>
              <li>CaptionGen does not claim ownership of content you generate using the Service</li>
              <li>You retain full responsibility for content published to third-party platforms</li>
            </ul>
          </div>

          <div className="legal-section">
            <h2>6. Third-Party Platform Integrations</h2>
            <p>
              The Service integrates with third-party social media platforms (such as TikTok and LinkedIn).
              Use of those platforms is subject to their own terms of service and privacy policies.
              CaptionGen is not affiliated with, endorsed by, or sponsored by any third-party platform.
            </p>
          </div>

          <div className="legal-section">
            <h2>7. Credits and Payments</h2>
            <p>
              Certain features require purchasing credits. All purchases are final and non-refundable except
              as required by applicable law. Credits do not expire and are tied to your account. Pricing
              is subject to change with reasonable notice.
            </p>
          </div>

          <div className="legal-section">
            <h2>8. Intellectual Property</h2>
            <p>
              The Service, including its design, code, and branding, is owned by CaptionGen and protected
              by applicable intellectual property laws. You may not copy, reproduce, or distribute any
              part of the Service without prior written consent.
            </p>
          </div>

          <div className="legal-section">
            <h2>9. Limitation of Liability</h2>
            <p>
              To the maximum extent permitted by law, CaptionGen shall not be liable for any indirect,
              incidental, special, or consequential damages arising from your use of the Service, including
              but not limited to loss of revenue, data, or business opportunities.
            </p>
          </div>

          <div className="legal-section">
            <h2>10. Termination</h2>
            <p>
              We reserve the right to suspend or terminate your account at our discretion if you violate
              these Terms. You may also delete your account at any time from your account settings.
            </p>
          </div>

          <div className="legal-section">
            <h2>11. Changes to Terms</h2>
            <p>
              We may update these Terms from time to time. Continued use of the Service after changes
              are posted constitutes your acceptance of the updated Terms. We will notify users of
              material changes via email or an in-app notice.
            </p>
          </div>

          <div className="legal-section">
            <h2>12. Contact</h2>
            <div className="legal-contact-box">
              <p>If you have questions about these Terms, please contact us:</p>
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
        <Link to="/privacy" style={{ color: 'var(--accent-indigo)', textDecoration: 'none' }}>
          Privacy Policy
        </Link>
      </footer>
    </div>
  )
}
