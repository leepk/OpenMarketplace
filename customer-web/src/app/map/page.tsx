import { MapExperience } from '@/components/map/MapExperience';
import { apiClient, type ListingDto } from '@/lib/api/apiClient';

export default async function MapPage() {
  let listings: ListingDto[] = [];
  try {
    const data: any = await apiClient.get('/listings?pageSize=60');
    listings = data.items ?? [];
  } catch {
    listings = [];
  }
  return <MapExperience listings={listings} />;
}
