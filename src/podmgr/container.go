package main

import "fmt"

func ensureJobContainer(podName string, imageName string) error {
	fmt.Printf("Adding Job Container %v to %v ...\n", imageName, podName)

	// TODO: support namespaces
	err := ensureEphemeralContainer("default", podName, "job-container", imageName, false)
	return err
}

func rmJobContainer(podName string, imageName string) error {
	fmt.Printf("Removing Job Container %v from %v ...\n", imageName, podName)

	// TODO

	return nil
}

func ensureDebugContainer(podName string, imageName string) error {
	fmt.Printf("Adding Job Container %v to %v ...\n", imageName, podName)

	// TODO: support namespaces
	err := ensureEphemeralContainer("default", podName, "debug-container", imageName, true)
	return err
}
