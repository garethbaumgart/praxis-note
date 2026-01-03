import { execSync } from 'child_process';
import { Client } from 'pg';
import path from 'path';

const projectRoot = path.resolve(__dirname, '../..');

export default async function globalSetup() {
  console.log('Starting E2E test infrastructure...');

  // In CI, database is provided by GitHub Actions services - skip Docker
  if (!process.env.CI) {
    try {
      execSync('docker compose -f docker-compose.e2e.yml up -d --wait', {
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
          ConnectionStrings__DefaultConnection:
            'Host=localhost;Port=5433;Database=praxisnote_e2e;Username=praxisnote;Password=testpassword',
        },
      }
    );
  } else {
    console.log('Skipping migrations in CI (handled by workflow)');
  }

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
        password: 'testpassword',
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
