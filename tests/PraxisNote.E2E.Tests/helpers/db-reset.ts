import { Client } from 'pg';
import { randomUUID } from 'crypto';
import { MockUser } from './mock-auth';

const connectionConfig = {
  host: 'localhost',
  port: 5433,
  database: 'praxisnote_e2e',
  user: 'praxisnote',
  password: 'e2eTestPassword',
};

export async function resetDatabase(): Promise<void> {
  const client = new Client(connectionConfig);
  await client.connect();

  try {
    // Truncate all tables in reverse dependency order
    await client.query(`
      TRUNCATE TABLE "Tasks" CASCADE;
      TRUNCATE TABLE "Users" CASCADE;
    `);
  } finally {
    await client.end();
  }
}

// Each test file should use a unique suffix to avoid interference
export async function seedTestUser(suffix: number = 1): Promise<MockUser> {
  const client = new Client(connectionConfig);
  await client.connect();

  try {
    const userId = randomUUID();
    const email = `e2e-test-${suffix}@example.com`;
    const name = `E2E Test User ${suffix}`;

    // Use upsert to handle parallel test execution safely
    await client.query(
      `
      INSERT INTO "Users" (
        "Id",
        "ExternalIdentity_Provider",
        "ExternalIdentity_ProviderId",
        "Email_Value",
        "Name",
        "AvatarUrl",
        "CreatedAt",
        "LastLoginAt"
      )
      VALUES ($1::uuid, 'MockAuth', $2, $3, $4, NULL, NOW(), NOW())
      ON CONFLICT ("Id") DO NOTHING
      `,
      [userId, userId, email, name]
    );

    return { userId, email, name };
  } finally {
    await client.end();
  }
}
