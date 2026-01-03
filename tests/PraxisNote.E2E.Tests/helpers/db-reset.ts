import { Client } from 'pg';

const connectionConfig = {
  host: 'localhost',
  port: 5433,
  database: 'praxisnote_e2e',
  user: 'praxisnote',
  password: 'testpassword',
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

export async function seedTestUser(): Promise<{ userId: string; email: string; name: string }> {
  const client = new Client(connectionConfig);
  await client.connect();

  try {
    const userId = '00000000-0000-0000-0000-000000000001';
    const email = 'e2e-test@example.com';
    const name = 'E2E Test User';

    // Check if user exists, if not create
    const result = await client.query(
      `SELECT "Id" FROM "Users" WHERE "Id" = $1`,
      [userId]
    );

    if (result.rows.length === 0) {
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
        `,
        [userId, userId, email, name]
      );
    }

    return { userId, email, name };
  } finally {
    await client.end();
  }
}
