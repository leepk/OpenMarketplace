export type CategoryCode =
  | 'vehicles'
  | 'property_rentals'
  | 'for_sale'
  | 'jobs'
  | 'services'
  | 'electronics'
  | 'home_garden'
  | 'community'
  | 'pets'
  | 'sports_outdoors';

export type CategoryDefinition = {
  code: CategoryCode;
  labelKey: string;
  iconKey: string;
  aliases: string[];
};

export const categoryDefinitions: CategoryDefinition[] = [
  { code: 'vehicles', labelKey: 'vehicles', iconKey: 'vehicle', aliases: ['vehicle', 'vehicles', 'car', 'cars', 'auto', 'autos', 'motorcycle', 'truck'] },
  { code: 'property_rentals', labelKey: 'propertyRentals', iconKey: 'rental', aliases: ['property', 'property rentals', 'rental', 'rentals', 'rent', 'real estate', 'apartment', 'house'] },
  { code: 'for_sale', labelKey: 'forSale', iconKey: 'sale', aliases: ['for sale', 'sale', 'market', 'buy sell', 'shopping'] },
  { code: 'jobs', labelKey: 'jobs', iconKey: 'jobs', aliases: ['job', 'jobs', 'career', 'hiring'] },
  { code: 'services', labelKey: 'services', iconKey: 'services', aliases: ['service', 'services', 'repair', 'cleaning'] },
  { code: 'electronics', labelKey: 'electronics', iconKey: 'electronics', aliases: ['electronic', 'electronics', 'phone', 'phones', 'computer', 'laptop'] },
  { code: 'home_garden', labelKey: 'homeGarden', iconKey: 'garden', aliases: ['home', 'garden', 'furniture', 'decor'] },
  { code: 'community', labelKey: 'community', iconKey: 'community', aliases: ['community', 'event', 'events'] },
  { code: 'pets', labelKey: 'pets', iconKey: 'pets', aliases: ['pet', 'pets', 'dog', 'cat'] },
  { code: 'sports_outdoors', labelKey: 'sportsOutdoors', iconKey: 'sports', aliases: ['sports', 'sport', 'outdoors', 'outdoor'] },
];

function clean(value?: string | null) {
  return (value ?? '')
    .trim()
    .toLowerCase()
    .replace(/&/g, 'and')
    .replace(/[_/]+/g, ' ')
    .replace(/-/g, ' ')
    .replace(/\s+/g, ' ');
}

export function normalizeCategoryCode(value?: string | null): CategoryCode | null {
  const normalized = clean(value);
  if (!normalized) return null;
  const underscored = normalized.replace(/\s+/g, '_') as CategoryCode;
  if (categoryDefinitions.some((item) => item.code === underscored)) return underscored;
  const found = categoryDefinitions.find((item) => item.aliases.some((alias) => normalized === clean(alias) || normalized.includes(clean(alias))));
  return found?.code ?? null;
}

export function getCategoryLabelKey(value?: string | null) {
  const code = normalizeCategoryCode(value);
  return categoryDefinitions.find((item) => item.code === code)?.labelKey;
}

export function getCategoryIconKey(value?: string | null) {
  const code = normalizeCategoryCode(value);
  return categoryDefinitions.find((item) => item.code === code)?.iconKey ?? 'category';
}

export function getCategoryRouteValue(value?: string | null) {
  return normalizeCategoryCode(value) ?? clean(value).replace(/\s+/g, '-');
}
