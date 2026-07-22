import Link from 'next/link';

type Section = {
  title: string;
  paragraphs?: string[];
  items?: string[];
};

export function LegalPage({
  eyebrow,
  title,
  updated,
  intro,
  sections,
}: {
  eyebrow: string;
  title: string;
  updated: string;
  intro: string;
  sections: Section[];
}) {
  return (
    <div className="legal-page-shell">
      <article className="legal-card">
        <div className="legal-heading">
          <span>{eyebrow}</span>
          <h1>{title}</h1>
          <p>{intro}</p>
          <small>Last updated: {updated}</small>
        </div>
        <div className="legal-content">
          {sections.map((section) => (
            <section key={section.title}>
              <h2>{section.title}</h2>
              {section.paragraphs?.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}
              {section.items && <ul>{section.items.map((item) => <li key={item}>{item}</li>)}</ul>}
            </section>
          ))}
        </div>
        <div className="legal-help-box">
          <strong>Questions?</strong>
          <span>Contact Vunoca at <a href="mailto:support@vunoca.com">support@vunoca.com</a>.</span>
          <Link href="/contact">Contact us</Link>
        </div>
      </article>
    </div>
  );
}
