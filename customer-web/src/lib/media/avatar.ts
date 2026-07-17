import { mediaUrl } from './url';

export const DEFAULT_AVATAR_PATH = '/avatars/avatar-1.svg';

export function avatarUrl(value?: string | null) {
  return mediaUrl(value) || DEFAULT_AVATAR_PATH;
}
