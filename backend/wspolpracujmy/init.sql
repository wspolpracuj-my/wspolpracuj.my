-- PostgreSQL 18 Configuration
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

-- Create ENUM type for status
CREATE TYPE status_enum AS ENUM ('Pending', 'Accepted', 'Rejected');


CREATE TABLE "Services" (
  "id" integer PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  "name" varchar NOT NULL
);

CREATE TABLE "Companies" (
  "TIN" varchar PRIMARY KEY,
  "name" varchar NOT NULL,
  "description" text,
  "website" varchar,
  "contact_email" varchar,
  "service_id" integer,
  "offer_id" integer,
  "location" varchar,
  "created_at" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
  CONSTRAINT "fk_companies_service" FOREIGN KEY ("service_id") REFERENCES "Services" ("id") DEFERRABLE INITIALLY IMMEDIATE,
  CONSTRAINT "fk_companies_offer" FOREIGN KEY ("offer_id") REFERENCES "Services" ("id") DEFERRABLE INITIALLY IMMEDIATE
);

CREATE TABLE "Users" (
  "id" integer PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  "mail" varchar NOT NULL,
  "password" varchar NOT NULL,
  "company_TIN" varchar,
  "verified" boolean DEFAULT false,
  CONSTRAINT "fk_users_company" FOREIGN KEY ("company_TIN") REFERENCES "Companies" ("TIN") DEFERRABLE INITIALLY IMMEDIATE
);

CREATE TABLE "Matches" (
  "id" integer PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  "company_TIN" varchar NOT NULL,
  "matched_company_TIN" varchar NOT NULL,
  "status" status_enum DEFAULT 'pending' NOT NULL,
  "created_at" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
  CONSTRAINT "fk_matches_company" FOREIGN KEY ("company_TIN") REFERENCES "Companies" ("TIN") DEFERRABLE INITIALLY IMMEDIATE,
  CONSTRAINT "fk_matches_matched_company" FOREIGN KEY ("matched_company_TIN") REFERENCES "Companies" ("TIN") DEFERRABLE INITIALLY IMMEDIATE,
  CONSTRAINT "ck_matches_different_companies" CHECK ("company_TIN" != "matched_company_TIN")
);

-- Create indexes for better query performance
CREATE INDEX "idx_users_mail" ON "Users" ("mail");
CREATE INDEX "idx_users_company_tin" ON "Users" ("company_TIN");
CREATE INDEX "idx_companies_service_id" ON "Companies" ("service_id");
CREATE INDEX "idx_companies_offer_id" ON "Companies" ("offer_id");
CREATE INDEX "idx_matches_company_tin" ON "Matches" ("company_TIN");
CREATE INDEX "idx_matches_matched_company_tin" ON "Matches" ("matched_company_TIN");
CREATE INDEX "idx_matches_status" ON "Matches" ("status");
