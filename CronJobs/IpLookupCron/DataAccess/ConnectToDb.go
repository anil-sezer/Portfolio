package DataAccess

import (
	"IpLookupCron/DataAccess/Entities/Postgres"
	"fmt"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"
	"gorm.io/gorm/logger"
	"os"
)

func DbContext() *gorm.DB {

	// Initialize the ORM database connection ('dsn' is your Postgres connection string).
	dsn := getConnectionString()
	db, err := gorm.Open(postgres.Open(dsn), &gorm.Config{Logger: logger.Default.LogMode(logger.Info)})
	if err != nil {
		panic("failed to connect to database")
	}

	return db
}

// KEEP THIS CLOSED
func AutoMigrate(db *gorm.DB) {

	// Automatically migrate your schema, creating the 'requests' table if it doesn't exist.
	var err = db.AutoMigrate(&Postgres.Request{})
	if err != nil {
		panic("failed to auto migrate")
	}
}

func getConnectionString() string {
	var connectionString = connectionStringFromEnvFile()

	if connectionString != "" {
		fmt.Println("DB Connection String:", connectionString)
		return connectionString
	}

	fmt.Println("DB Connection String:", connectionString)
	return connectionString
}

func connectionStringFromEnvFile() string {

	connectionString, exists := os.LookupEnv("DB_CONNECTION_STRING")
	if !exists {
		return ""
	} else {
		return connectionString
	}
}
