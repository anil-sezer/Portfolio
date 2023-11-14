package Postgres

import "time"

type Request struct {
	ID             string `gorm:"primaryKey"`
	UserAgent      string
	AcceptLanguage string
	ClientIP       string
	Country        string
	City           string
	CreationTime   time.Time
}

// TableName overrides the table name used by Request to, e.g. "requests"
func (Request) TableName() string {
	return "public.request" // This explicitly sets the table name including the schema
}
