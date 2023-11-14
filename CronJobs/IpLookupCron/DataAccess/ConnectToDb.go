package DataAccess

import (
	"IpLookupCron/DataAccess/Entities/Postgres"
	"context"
	"fmt"
	"github.com/jackc/pgx/v4"
)

// Function to establish a connection to the database
func connectDB(connString string) (*pgx.Conn, error) {
	conn, err := pgx.Connect(context.Background(), connString)
	if err != nil {
		return nil, fmt.Errorf("unable to connect to the database: %w", err)
	}
	return conn, nil
}

// Function to query all requests from the database
func queryRequests(conn *pgx.Conn) ([]Postgres.Request, error) {
	rows, err := conn.Query(context.Background(), "SELECT client_ip FROM public.request WHERE country = ''")
	if err != nil {
		return nil, fmt.Errorf("query failed: %w", err)
	}
	defer rows.Close()

	var requests []Postgres.Request
	for rows.Next() {
		var r Postgres.Request
		if err := rows.Scan(&r.ClientIP); err != nil {
			return nil, fmt.Errorf("error scanning row: %w", err)
		}
		requests = append(requests, r)
	}

	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("error during rows iteration: %w", err)
	}

	return requests, nil
}
