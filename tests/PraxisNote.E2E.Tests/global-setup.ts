import { execSync } from 'child_process';
import { Client } from 'pg';
import path from 'path';
import { resetDatabase } from './helpers/db-reset';

const projectRoot = path.resolve(__dirname, '../..');
const E2E_CONNECTION_STRING =
  'Host=localhost;Port=5433;Database=praxisnote_e2e;Username=praxisnote;Password=e2eTestPassword';

export default async function globalSetup() {
  console.log('Starting E2E test infrastructure...');

  // In CI, database is provided by GitHub Actions services - skip Docker
  if (!process.env.CI) {
    try {
      execSync('docker compose --profile e2e up -d --wait', {
        cwd: projectRoot,
        stdio: 'inherit',
      });
    } catch (error) {
      console.error('Failed to start Docker container. Is Docker running?');
      throw error;
    }
  } else {
    console.log('Skipping Docker in CI (using GitHub Actions service)');
  }

  // Wait for database to be ready
  await waitForDatabase();

  // Run migrations (skip in CI - handled by workflow)
  if (!process.env.CI) {
    console.log('Running database migrations...');
    execSync(
      'dotnet ef database update -p src/PraxisNote.Infrastructure -s src/PraxisNote.Web',
      {
        cwd: projectRoot,
        stdio: 'inherit',
        env: {
          ...process.env,
          ConnectionStrings__DefaultConnection: E2E_CONNECTION_STRING,
        },
      }
    );
  } else {
    console.log('Skipping migrations in CI (handled by workflow)');
  }

  // Reset database to ensure clean state for all tests
  console.log('Resetting database...');
  await resetDatabase();

  console.log('E2E infrastructure ready');
}

async function waitForDatabase(maxAttempts = 30): Promise<void> {
  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      const client = new Client({
        host: 'localhost',
        port: 5433,
        database: 'praxisnote_e2e',
        user: 'praxisnote',
        password: 'e2eTestPassword',
      });
      await client.connect();
      await client.end();
      console.log('Database is ready');
      return;
    } catch {
      console.log(`Waiting for database... (attempt ${attempt}/${maxAttempts})`);
      await new Promise((resolve) => setTimeout(resolve, 1000));
    }
  }
  throw new Error('Database failed to start');
}
