package main

import (
	"fmt"

	v1 "k8s.io/apimachinery/pkg/apis/meta/v1"
)

// https://stackoverflow.com/questions/48366173/how-do-i-build-a-static-go-binary-for-the-docker-alpine-image
// https://rollout.io/blog/building-minimal-docker-containers-for-go-applications/

func main() {
	fmt.Println("Hello world!")

	v1Client, err := getCoreV1()
	if err != nil {
		panic(err)
	}

	pods, _ := v1Client.Pods("").List(v1.ListOptions{})
	fmt.Printf("There are %d pods in the cluster\n", len(pods.Items))

}
