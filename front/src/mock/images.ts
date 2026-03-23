export const imageUrls = {
  hero: {
    slide1: 'https://images.unsplash.com/photo-1766299892693-2370a8d47e23?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080',
    slide2: 'https://images.unsplash.com/photo-1587010580103-fd86b8ea14ca?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080',
    slide3: 'https://images.unsplash.com/photo-1663229049147-30f47be043ea?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080',
  },
  categories: {
    imaging: 'https://images.unsplash.com/photo-1587010580103-fd86b8ea14ca?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    surgical: 'https://images.unsplash.com/photo-1560269941-141b145a1b57?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    monitoring: 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    sterilization: 'https://images.unsplash.com/photo-1758653500328-1c4474a8adfe?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    diagnostic: 'https://images.unsplash.com/photo-1766299892549-b56b257d1ddd?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    furniture: 'https://images.unsplash.com/photo-1710074213374-e68503a1b795?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    respiratory: 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    ppe: 'https://images.unsplash.com/photo-1758653500328-1c4474a8adfe?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  },
  products: {
    'xray-machine': 'https://images.unsplash.com/photo-1587010580103-fd86b8ea14ca?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'ultrasound-machine': 'https://images.unsplash.com/photo-1663229049147-30f47be043ea?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'patient-monitor': 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'autoclave': 'https://images.unsplash.com/photo-1758653500328-1c4474a8adfe?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'surgical-scalpel': 'https://images.unsplash.com/photo-1560269941-141b145a1b57?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'examination-table': 'https://images.unsplash.com/photo-1766299892693-2370a8d47e23?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'blood-analyzer': 'https://images.unsplash.com/photo-1766299892549-b56b257d1ddd?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'ecg-machine': 'https://images.unsplash.com/photo-1766299892549-b56b257d1ddd?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'surgical-light': 'https://images.unsplash.com/photo-1560269941-141b145a1b57?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'ventilator': 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'microscope': 'https://images.unsplash.com/photo-1766299892549-b56b257d1ddd?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'thermometer': 'https://images.unsplash.com/photo-1766299892549-b56b257d1ddd?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'hospital-bed': 'https://images.unsplash.com/photo-1710074213374-e68503a1b795?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'air-purifier': 'https://images.unsplash.com/photo-1758653500328-1c4474a8adfe?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'pulse-oximeter': 'https://images.unsplash.com/photo-1721114989769-0423619f03d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'forceps': 'https://images.unsplash.com/photo-1560269941-141b145a1b57?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'ct-scanner': 'https://images.unsplash.com/photo-1587010580103-fd86b8ea14ca?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
    'medical-cart': 'https://images.unsplash.com/photo-1766299892693-2370a8d47e23?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=600',
  },
};

export function getProductImage(imageKey: string): string {
  return imageUrls.products[imageKey as keyof typeof imageUrls.products] || imageUrls.products['examination-table'];
}

export function getCategoryImage(catId: string): string {
  const map: Record<string, string> = {
    'cat-1': imageUrls.categories.imaging,
    'cat-2': imageUrls.categories.surgical,
    'cat-3': imageUrls.categories.monitoring,
    'cat-4': imageUrls.categories.sterilization,
    'cat-5': imageUrls.categories.diagnostic,
    'cat-6': imageUrls.categories.furniture,
    'cat-7': imageUrls.categories.respiratory,
    'cat-8': imageUrls.categories.ppe,
  };
  return map[catId] || imageUrls.categories.imaging;
}
