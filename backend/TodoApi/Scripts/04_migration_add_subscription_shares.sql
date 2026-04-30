-- Migration to add subscription_shares table (idempotent)

CREATE TABLE IF NOT EXISTS subscription_shares (
    id UUID PRIMARY KEY,
    subscription_id UUID NOT NULL REFERENCES subscriptions(id) ON DELETE CASCADE,
    shared_with_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    shared_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (subscription_id, shared_with_user_id)
);

CREATE INDEX IF NOT EXISTS idx_subscription_shares_shared_with
    ON subscription_shares (shared_with_user_id, subscription_id);

CREATE INDEX IF NOT EXISTS idx_subscription_shares_shared_by
    ON subscription_shares (shared_by_user_id, subscription_id);
