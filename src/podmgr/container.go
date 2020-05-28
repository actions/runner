package main

import "fmt"

func addJobContainer(podName string, imageName string) error {
	fmt.Printf("Adding Job Container %v to %v ...\n", imageName, podName)

	// TODO: support namespaces
	err := createEphemeralContainer(podName, imageName, "default")
	return err
}

func rmJobContainer(podName string, imageName string) error {
	fmt.Printf("Removing Job Container %v from %v ...\n", imageName, podName)
	return nil
}
