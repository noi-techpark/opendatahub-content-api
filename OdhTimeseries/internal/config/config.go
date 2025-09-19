package config

import (
	"time"

	"github.com/kelseyhightower/envconfig"
)

type Config struct {
	Database DatabaseConfig `envconfig:"DB"`
	Server   ServerConfig   `envconfig:"SERVER"`
	Logging  LoggingConfig  `envconfig:"LOG"`
}

type DatabaseConfig struct {
	Host     string `envconfig:"DB_HOST" default:"localhost"`
	Port     int    `envconfig:"DB_PORT" default:"5556"`
	Name     string `envconfig:"DB_NAME" default:"postgres"`
	User     string `envconfig:"DB_USER" default:"bdp"`
	Password string `envconfig:"DB_PASSWORD" default:"password"`
	Schema   string `envconfig:"DB_SCHEMA" default:"intimev3"`
	SSLMode  string `envconfig:"DB_SSL_MODE" default:"disable"`
}

type ServerConfig struct {
	Port            int           `envconfig:"SERVER_PORT" default:"8080"`
	ReadTimeout     time.Duration `envconfig:"SERVER_READ_TIMEOUT" default:"10s"`
	WriteTimeout    time.Duration `envconfig:"SERVER_WRITE_TIMEOUT" default:"10s"`
	ShutdownTimeout time.Duration `envconfig:"SERVER_SHUTDOWN_TIMEOUT" default:"5s"`
}

type LoggingConfig struct {
	Level string `envconfig:"LEVEL" default:"info"`
}

func Load() (*Config, error) {
	var cfg Config
	if err := envconfig.Process("", &cfg); err != nil {
		return nil, err
	}
	return &cfg, nil
}
