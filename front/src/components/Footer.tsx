'use client';

import Link from 'next/link';
import { Facebook, Twitter, Linkedin, Mail, Phone, MapPin } from 'lucide-react';
import { useI18n } from '@/context/i18n-context';

export function Footer() {
  const { t } = useI18n();

  return (
    <footer className="hidden md:block bg-brand-dark text-white mt-16">
      <div className="container mx-auto px-4 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Company Info */}
          <div>
            <h3 className="text-xl mb-4">Althea Systems</h3>
            <p className="text-gray-300 text-sm mb-4" style={{ fontFamily: 'var(--font-body)' }}>
              {t('footer.company_desc')}
            </p>
            <div className="flex gap-3">
              <a href="#" className="hover:text-brand-primary transition-colors" aria-label="Facebook">
                <Facebook className="w-5 h-5" />
              </a>
              <a href="#" className="hover:text-brand-primary transition-colors" aria-label="Twitter">
                <Twitter className="w-5 h-5" />
              </a>
              <a href="#" className="hover:text-brand-primary transition-colors" aria-label="LinkedIn">
                <Linkedin className="w-5 h-5" />
              </a>
            </div>
          </div>

          {/* Quick Links */}
          <div>
            <h4 className="mb-4">{t('footer.quick_links')}</h4>
            <ul className="space-y-2 text-sm">
              <li><Link href="/" className="text-gray-300 hover:text-brand-primary transition-colors">{t('nav.home')}</Link></li>
              <li><Link href="/categories" className="text-gray-300 hover:text-brand-primary transition-colors">{t('nav.categories')}</Link></li>
              <li><Link href="/a-propos" className="text-gray-300 hover:text-brand-primary transition-colors">{t('nav.about')}</Link></li>
              <li><Link href="/contact" className="text-gray-300 hover:text-brand-primary transition-colors">{t('nav.contact')}</Link></li>
            </ul>
          </div>

          {/* Legal */}
          <div>
            <h4 className="mb-4">{t('footer.legal_links')}</h4>
            <ul className="space-y-2 text-sm">
              <li><Link href="/cgu" className="text-gray-300 hover:text-brand-primary transition-colors">{t('nav.cgu')}</Link></li>
              <li><Link href="/mentions-legales" className="text-gray-300 hover:text-brand-primary transition-colors">{t('nav.legal')}</Link></li>
            </ul>
          </div>

          {/* Contact */}
          <div>
            <h4 className="mb-4">{t('footer.contact')}</h4>
            <ul className="space-y-3 text-sm">
              <li className="flex items-start gap-2">
                <MapPin className="w-5 h-5 text-brand-primary shrink-0 mt-0.5" />
                <span className="text-gray-300">1234 Medical Plaza<br />75008 Paris, France</span>
              </li>
              <li className="flex items-center gap-2">
                <Phone className="w-5 h-5 text-brand-primary" />
                <span className="text-gray-300">+33 (0)1 23 45 67 89</span>
              </li>
              <li className="flex items-center gap-2">
                <Mail className="w-5 h-5 text-brand-primary" />
                <span className="text-gray-300">contact@altheasystems.com</span>
              </li>
            </ul>
          </div>
        </div>

        <div className="border-t border-white/10 mt-8 pt-8 text-center text-sm text-gray-400">
          <p>{t('footer.rights')}</p>
        </div>
      </div>
    </footer>
  );
}
