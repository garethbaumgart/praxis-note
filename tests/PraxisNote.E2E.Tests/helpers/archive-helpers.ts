import { Client } from 'pg';
import { randomUUID } from 'crypto';

const connectionConfig = {
  host: 'localhost',
  port: 5433,
  database: 'praxisnote_e2e',
  user: 'praxisnote',
  password: 'e2eTestPassword',
};

// Archive threshold must match appsettings.json Tasks:ArchiveThresholdDays
const ARCHIVE_THRESHOLD_DAYS = 2;

/**
 * Seeds a task that is considered "archived" (completed more than 2 days ago)
 * @param userId The user ID to associate the task with
 * @param title The task title
 * @param daysAgo How many days ago the task was completed (must be > 2 for archiving)
 * @returns The created task ID
 */
export async function seedArchivedTask(
  userId: string,
  title: string,
  daysAgo: number = 5
): Promise<string> {
  if (daysAgo <= ARCHIVE_THRESHOLD_DAYS) {
    throw new Error(`daysAgo must be > ${ARCHIVE_THRESHOLD_DAYS} for an archived task`);
  }

  const client = new Client(connectionConfig);
  await client.connect();

  try {
    const taskId = randomUUID();
    const now = new Date();
    const completedAt = new Date(now.getTime() - daysAgo * 24 * 60 * 60 * 1000);
    const startedAt = new Date(completedAt.getTime() - 60 * 60 * 1000); // 1 hour before completed
    const createdAt = new Date(startedAt.getTime() - 60 * 60 * 1000); // 1 hour before started

    await client.query(
      `
      INSERT INTO "Tasks" (
        "Id",
        "UserId",
        "Title",
        "Status",
        "Position",
        "CreatedAt",
        "UpdatedAt",
        "StartedAt",
        "CompletedAt",
        "DueDate",
        "Comments",
        "LabelIds"
      )
      VALUES ($1::uuid, $2::uuid, $3, 'Done', 0, $4, $5, $6, $7, NULL, '[]'::jsonb, '[]'::jsonb)
      `,
      [taskId, userId, title, createdAt.toISOString(), completedAt.toISOString(), startedAt.toISOString(), completedAt.toISOString()]
    );

    return taskId;
  } finally {
    await client.end();
  }
}

/**
 * Seeds a recently completed task (within the last 2 days, not archived)
 * @param userId The user ID to associate the task with
 * @param title The task title
 * @returns The created task ID
 */
export async function seedRecentDoneTask(
  userId: string,
  title: string
): Promise<string> {
  const client = new Client(connectionConfig);
  await client.connect();

  try {
    const taskId = randomUUID();
    const now = new Date();
    const completedAt = new Date(now.getTime() - 1 * 24 * 60 * 60 * 1000); // 1 day ago (within threshold)
    const startedAt = new Date(completedAt.getTime() - 60 * 60 * 1000); // 1 hour before completed
    const createdAt = new Date(startedAt.getTime() - 60 * 60 * 1000); // 1 hour before started

    await client.query(
      `
      INSERT INTO "Tasks" (
        "Id",
        "UserId",
        "Title",
        "Status",
        "Position",
        "CreatedAt",
        "UpdatedAt",
        "StartedAt",
        "CompletedAt",
        "DueDate",
        "Comments",
        "LabelIds"
      )
      VALUES ($1::uuid, $2::uuid, $3, 'Done', 0, $4, $5, $6, $7, NULL, '[]'::jsonb, '[]'::jsonb)
      `,
      [taskId, userId, title, createdAt.toISOString(), completedAt.toISOString(), startedAt.toISOString(), completedAt.toISOString()]
    );

    return taskId;
  } finally {
    await client.end();
  }
}
