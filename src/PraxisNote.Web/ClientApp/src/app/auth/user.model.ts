export interface UserProfile {
  id: string;
  name: string;
  icon: string | null;
  isDefault: boolean;
}

export interface User {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
  provider: string;
  profiles: UserProfile[];
}
