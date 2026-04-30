-- Conéctese a la base subscription_tracker_db y ejecute este script.

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    email VARCHAR(320) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS subscriptions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    category VARCHAR(100) NOT NULL,
    billing_cycle VARCHAR(20) NOT NULL,
    amount NUMERIC(12,2) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    next_billing_date DATE NOT NULL,
    notes TEXT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS subscription_shares (
    id UUID PRIMARY KEY,
    subscription_id UUID NOT NULL REFERENCES subscriptions(id) ON DELETE CASCADE,
    shared_with_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    shared_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (subscription_id, shared_with_user_id)
);

CREATE INDEX IF NOT EXISTS idx_subscriptions_user_next_billing
    ON subscriptions (user_id, next_billing_date ASC);

CREATE INDEX IF NOT EXISTS idx_subscriptions_user_category
    ON subscriptions (user_id, category);

CREATE INDEX IF NOT EXISTS idx_subscriptions_user_billing_cycle
    ON subscriptions (user_id, billing_cycle);

CREATE INDEX IF NOT EXISTS idx_subscription_shares_shared_with
    ON subscription_shares (shared_with_user_id, subscription_id);

CREATE INDEX IF NOT EXISTS idx_subscription_shares_shared_by
    ON subscription_shares (shared_by_user_id, subscription_id);
