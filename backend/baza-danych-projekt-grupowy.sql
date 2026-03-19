CREATE TABLE "Users" (
  "id" integer,
  "mail" varchar,
  "password" varchar,
  "company_TIN" varchar,
  "verified" bool
);

CREATE TABLE "Services" (
  "id" integer PRIMARY KEY,
  "name" varchar
);

CREATE TABLE "Companies" (
  "TIN" varchar PRIMARY KEY,
  "name" varchar,
  "description" text,
  "website" varchar,
  "contact_email" varchar,
  "service_id" integer,
  "offer_id" integer,
  "location" varchar,
  "created_at" TIMESTAMP
);

CREATE TABLE "Matches" (
  "id" integer,
  "company_TIN" integer,
  "matched_company_TIN" integer,
  "status" "ENUM(pending,accepted,rejected)",
  "created_at" TIMESTAMP
);

ALTER TABLE "Matches" ADD FOREIGN KEY ("company_TIN") REFERENCES "Companies" ("TIN") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Services" ADD FOREIGN KEY ("id") REFERENCES "Companies" ("service_id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Services" ADD FOREIGN KEY ("id") REFERENCES "Companies" ("offer_id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Matches" ADD FOREIGN KEY ("matched_company_TIN") REFERENCES "Companies" ("TIN") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "Companies" ADD FOREIGN KEY ("TIN") REFERENCES "Users" ("company_TIN") DEFERRABLE INITIALLY IMMEDIATE;
