export interface MockUser {
  userId: string;
  email: string;
  name: string;
}

export function getMockAuthHeader(user: MockUser): Record<string, string> {
  return {
    'X-Mock-User': `${user.email}|${user.name}|${user.userId}`,
  };
}
