import { StatusBadge } from './AdminCommon';
import { AdminActionGroup, AdminButton } from './AdminControls';
import { apiClient } from '../../lib/api/apiClient';
import { categoryInitial, formatPrice } from '../../utils/format';

export function ListingAdminCard({ item, onChanged }: { item: any; onChanged: () => void }) {
  const image = item.imageUrl || item.ImageUrl || '';
  const status = item.moderationStatus || item.status || 'Pending';
  async function mod(decision: string) {
    const reason = decision === 'Approved' ? 'Approved by admin' : prompt('Reject reason?') || 'Rejected by admin';
    try {
      await apiClient.post(`/admin/listings/${item.id}/moderate`, { decision, reason });
      onChanged();
    } catch (e) {
      alert((e as Error).message);
    }
  }
  return (
    <article className="listing-review-card">
      <div className="review-image" style={image ? { backgroundImage: `url(${image})` } : undefined}>
        {!image && <span>{categoryInitial(item.category?.name || item.categoryName || item.category)}</span>}
      </div>
      <div className="review-body">
        <div className="review-top"><b>{item.title}</b><StatusBadge value={status} /></div>
        <p>{formatPrice(item.currency, item.price)} · {item.city || item.location || ''} {item.state || ''}</p>
        <small>{item.category?.name || item.categoryName || item.category || 'Category'} · Seller: {item.seller?.name || item.sellerName || item.seller || 'User'}</small>
        {item.moderationReason && <small className="reason">Reason: {item.moderationReason}</small>}
        <AdminActionGroup className="review-actions"><AdminButton variant="primary" onClick={() => mod('Approved')}>Approve</AdminButton><AdminButton variant="danger" onClick={() => mod('Rejected')}>Reject</AdminButton><AdminButton onClick={() => window.open(`/listings/${item.id}`, '_blank')}>Preview</AdminButton></AdminActionGroup>
      </div>
    </article>
  );
}
