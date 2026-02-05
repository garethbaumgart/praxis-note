using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceLowercaseTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Identify duplicate tags (same user, same LOWER(name)) and merge them.
            // For each duplicate group, keep the earliest tag (by CreatedAt) as the survivor.
            // Replace references to deleted tags in Tasks, Notes, and Meetings TagIds columns.
            migrationBuilder.Sql("""
                -- Merge duplicate tags and update references in a single transaction.
                -- TagIds columns are stored as JSON-serialized text arrays of UUIDs, e.g. '["uuid1","uuid2"]'

                DO $$
                DECLARE
                    dup RECORD;
                    old_id uuid;
                    tbl text;
                BEGIN
                    -- For each group of duplicate tags (same user + lowercased name), keep the earliest one
                    FOR dup IN
                        SELECT "UserId", LOWER("Name") AS lower_name,
                               (ARRAY_AGG("Id" ORDER BY "CreatedAt" ASC))[1] AS survivor_id,
                               ARRAY_REMOVE(ARRAY_AGG("Id" ORDER BY "CreatedAt" ASC),
                                            (ARRAY_AGG("Id" ORDER BY "CreatedAt" ASC))[1]) AS duplicate_ids
                        FROM "Tags"
                        GROUP BY "UserId", LOWER("Name")
                        HAVING COUNT(*) > 1
                    LOOP
                        -- For each duplicate tag that will be deleted
                        FOREACH old_id IN ARRAY dup.duplicate_ids
                        LOOP
                            -- Replace old_id with survivor_id in TagIds JSON arrays across all entity tables
                            FOREACH tbl IN ARRAY ARRAY['Tasks', 'Notes', 'Meetings']
                            LOOP
                                -- Update rows that contain the old tag ID
                                EXECUTE format(
                                    'UPDATE %I SET "TagIds" = (
                                        SELECT jsonb_agg(DISTINCT elem)::text
                                        FROM jsonb_array_elements(
                                            REPLACE("TagIds", %L, %L)::jsonb
                                        ) AS elem
                                    )
                                    WHERE "TagIds"::jsonb @> %L::jsonb',
                                    tbl,
                                    old_id::text,
                                    dup.survivor_id::text,
                                    jsonb_build_array(old_id::text)
                                );
                            END LOOP;
                        END LOOP;

                        -- Delete the duplicate tag rows
                        DELETE FROM "Tags" WHERE "Id" = ANY(dup.duplicate_ids);
                    END LOOP;
                END $$;

                -- Step 2: Lowercase all remaining tag names
                UPDATE "Tags" SET "Name" = LOWER("Name") WHERE "Name" != LOWER("Name");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data migration — original casing cannot be restored
        }
    }
}
