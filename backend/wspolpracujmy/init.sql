CREATE TABLE "Users" (
  "id" integer PRIMARY KEY,
  "name" varchar NOT NULL,
  "surname" varchar NOT NULL,
  "role" "ENUM(admin,student,company)" NOT NULL,
  "login" varchar UNIQUE NOT NULL,
  "password" varchar NOT NULL
);

CREATE TABLE "Companies" (
  "id" integer PRIMARY KEY,
  "user_id" integer UNIQUE NOT NULL,
  "company_name" varchar NOT NULL,
  "contact_email" varchar
);

CREATE TABLE "Students" (
  "id" integer PRIMARY KEY,
  "user_id" integer UNIQUE NOT NULL,
  "group_id" integer NOT NULL,
  "email" varchar UNIQUE NOT NULL
);

CREATE TABLE "Groups" (
  "id" integer PRIMARY KEY,
  "name" varchar NOT NULL,
  "project_id" integer NOT NULL,
  "is_accepted" "ENUM(pending,accepted,declined)" NOT NULL,
  "leader_id" integer NOT NULL,
  "number_of_members" integer NOT NULL
);

CREATE TABLE "Project" (
  "id" integer PRIMARY KEY,
  "company_id" integer NOT NULL,
  "topic" varchar NOT NULL,
  "description" varchar,
  "project_goal" varchar,
  "work_scope" varchar,
  "needed_technologies" varchar,
  "created_at" timestamp DEFAULT (CURRENT_TIMESTAMP) NOT NULL,
  "max_groups" integer,
  "max_number_group_members" integer NOT NULL,
  "meeting_type_id" integer NOT NULL,
  "partnership_type" varchar,
  "language_doc" "ENUM(polish,english)" NOT NULL,
  "notes" varchar,
  "priority" "ENUM(1,2,3,4,5)" NOT NULL
);

CREATE TABLE "Files" (
  "id" uuid PRIMARY KEY,
  "user_id" integer NOT NULL,
  "original_name" varchar NOT NULL,
  "gcs_bucket" text NOT NULL,
  "gcs_object_name" varchar NOT NULL,
  "content_type" varchar NOT NULL,
  "size_bytes" bigint NOT NULL,
  "created_at" timestamp DEFAULT (CURRENT_TIMESTAMP) NOT NULL
);

CREATE TABLE "GroupFiles" (
  "group_id" integer NOT NULL,
  "file_id" uuid NOT NULL
);

CREATE TABLE "Comments" (
  "id" integer PRIMARY KEY,
  "user_id" integer NOT NULL,
  "project_id" integer NOT NULL,
  "content" varchar NOT NULL,
  "created_at" timestamp DEFAULT (CURRENT_TIMESTAMP) NOT NULL
);

CREATE TABLE "Responses" (
  "id" integer PRIMARY KEY,
  "comment_id" integer NOT NULL,
  "user_id" integer NOT NULL,
  "content" varchar NOT NULL,
  "created_at" timestamp DEFAULT (CURRENT_TIMESTAMP) NOT NULL
);

CREATE TABLE "Tags" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "ProjectTags" (
  "project_id" integer NOT NULL,
  "tag_id" integer NOT NULL
  PRIMARY KEY ("project_id", "tag_id"),
);

CREATE TABLE "Notifications" (
  "id" integer PRIMARY KEY,
  "user_id" integer NOT NULL,
  "content" varchar NOT NULL,
  "status" "ENUM(not-read,read)" NOT NULL,
);

CREATE TABLE "CalendarEvents" (
  "id" integer PRIMARY KEY,
  "time" timestamp NOT NULL,
  "name" varchar NOT NULL,
  "group_id" integer NOT NULL
);

CREATE TABLE "Meeting_types" (
  "id" integer PRIMARY KEY,
  "type" varchar NOT NULL
);

ALTER TABLE "Companies" ADD FOREIGN KEY ("user_id") REFERENCES "Users" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Students" ADD FOREIGN KEY ("user_id") REFERENCES "Users" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Students" ADD FOREIGN KEY ("group_id") REFERENCES "Groups" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Groups" ADD FOREIGN KEY ("project_id") REFERENCES "Projects" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Groups" ADD FOREIGN KEY ("leader_id") REFERENCES "Students" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Projects" ADD FOREIGN KEY ("company_id") REFERENCES "Company" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Projects" ADD FOREIGN KEY ("meeting_type_id") REFERENCES "Meeting_type" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Files" ADD FOREIGN KEY ("user_id") REFERENCES "Users" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "GroupFiles" ADD FOREIGN KEY ("group_id") REFERENCES "Groups" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "GroupFiles" ADD FOREIGN KEY ("file_id") REFERENCES "Files" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Comments" ADD FOREIGN KEY ("user_id") REFERENCES "Users" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Comments" ADD FOREIGN KEY ("project_id") REFERENCES "Projects" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Responses" ADD FOREIGN KEY ("comment_id") REFERENCES "Comments" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Responses" ADD FOREIGN KEY ("user_id") REFERENCES "Users" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "ProjectTags" ADD FOREIGN KEY ("project_id") REFERENCES "Projects" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "ProjectTags" ADD FOREIGN KEY ("tag_id") REFERENCES "Tags" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Notifications" ADD FOREIGN KEY ("user_id") REFERENCES "Users" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Calendar" ADD FOREIGN KEY ("group_id") REFERENCES "Groups" ("id") DEFERRABLE INITIALLY IMMEDIATE;
