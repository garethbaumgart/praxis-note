import { execSync } from 'child_process';
import { Client } from 'pg';

const projectRoot = process.cwd().replace('/tests/PraxisNote.E2E.Tests', '');

export default async function globalSetup() {
  console.log('Starting E2E test infrastructure...');

  // Start PostgreSQL E2E container
  try {
    execSync('docker compose -f docker-compose.e2e.yml up -d --wait', {
      cwd: projectRoot,
      stdio: 'inherit',
    });
  } catch (error) {
    console.error('Failed to start Docker container. Is Docker running?');
    throw error;
  }

  // Wait for database to be ready
  await waitForDatabase();

  // Run migrations
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
