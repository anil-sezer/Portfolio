package Domain

import (
	"bufio"
	"fmt"
	"github.com/joho/godotenv"
	"log"
	"os"
)

func GetWorkingDirectory() string {
	dir, err := os.Getwd()
	if err != nil {
		log.Fatal(err)
	}
	fmt.Println("Current working directory:", dir)

	return dir
}

func CheckIfFileExists(fileName string) bool {

	if _, err := os.Stat(fileName); err != nil {
		if os.IsNotExist(err) {
			fmt.Printf("File %s does not exist in the current directory.\n", fileName)
		} else {
			// Other error, might be a permission issue or something else
			fmt.Printf("Error checking file: %s\n", err)
		}
		return false

	} else {
		fmt.Printf("File %s exists in the current directory.\n", fileName)
		return true
	}
}

func PrintFilesContents(fileName string) {
	file, err := os.Open(fileName)
	if err != nil {
		log.Fatalf("failed opening file: %s", err)
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	scanner.Split(bufio.ScanLines)
	fmt.Println("Contents of .env file:")
	for scanner.Scan() {
		fmt.Println(scanner.Text())
	}

	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}
}

func LoadEnvFile() {
	if err := godotenv.Load(); err != nil {
		log.Fatal("Error loading .env file", err)
	}
}
