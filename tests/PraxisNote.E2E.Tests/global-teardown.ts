import { execSync } from 'child_process';
import path from 'path';

const projectRoot = path.resolve(__dirname, '../..');

export default async function globalTeardown() {
  if (!process.env.KEEP_CONTAINERS) {
    console.log('Stopping E2E test infrastructure...');
    try {
      execSync('docker compose -f docker-compose.e2e.yml down', {
        cwd: projectRoot,
        stdio: 'inherit',
      });
    } catch (error) {
      console.warn('Failed to stop Docker containers:', error);
    }
  } else {
    console.log('KEEP_CONTAINERS is set, leaving containers running');
  }
}
