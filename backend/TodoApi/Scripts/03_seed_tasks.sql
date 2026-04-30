INSERT INTO users (id, email, password_hash, created_at)
VALUES
    (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'demo@tracker.app',
        'AAECAwQFBgcICQoLDA0ODw==',
        NOW()
    )
ON CONFLICT (id) DO NOTHING;

INSERT INTO subscriptions (
    id,
    user_id,
    name,
    category,
    billing_cycle,
    amount,
    currency,
    next_billing_date,
    notes,
    created_at,
    updated_at
)
VALUES
    (
        '11111111-1111-1111-1111-111111111111',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'Netflix',
        'Streaming',
        'monthly',
        12.99,
        'USD',
        CURRENT_DATE + INTERVAL '7 day',
        'Plan estándar',
        NOW(),
        NOW()
    ),
    (
        '22222222-2222-2222-2222-222222222222',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'Spotify',
        'Music',
        'monthly',
        9.99,
        'USD',
        CURRENT_DATE + INTERVAL '12 day',
        'Cuenta personal',
        NOW(),
        NOW()
    ),
    (
        '33333333-3333-3333-3333-333333333333',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'Amazon Prime',
        'Shopping',
        'yearly',
        119.00,
        'USD',
        CURRENT_DATE + INTERVAL '25 day',
        'Facturación anual',
        NOW(),
        NOW()
    )
ON CONFLICT (id) DO NOTHING;
