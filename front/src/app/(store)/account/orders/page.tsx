'use client';

import { useRouter } from 'next/navigation';
import { useEffect } from 'react';

export default function AccountOrdersRedirect() {
  const router = useRouter();
  useEffect(() => {
    router.replace('/account?tab=orders');
  }, [router]);
  return null;
}
