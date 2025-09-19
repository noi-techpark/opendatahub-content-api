package database

import (
	"database/sql"
	"fmt"

	_ "github.com/lib/pq"
	"timeseries-api/internal/config"
)

type DB struct {
	*sql.DB
	Schema string
}

func Connect(cfg config.DatabaseConfig) (*DB, error) {
	dsn := fmt.Sprintf("host=%s port=%d user=%s password=%s dbname=%s sslmode=%s",
		cfg.Host, cfg.Port, cfg.User, cfg.Password, cfg.Name, cfg.SSLMode)

	db, err := sql.Open("postgres", dsn)
	if err != nil {
		return nil, fmt.Errorf("failed to open database connection: %w", err)
	}

	if err := db.Ping(); err != nil {
		return nil, fmt.Errorf("failed to ping database: %w", err)
	}

	return &DB{
		DB:     db,
		Schema: cfg.Schema,
	}, nil
}

func (db *DB) Close() error {
	return db.DB.Close()
}
